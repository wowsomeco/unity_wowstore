#if !TARGET_OS_TV

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

/**
 * Helper method to create C string copy
 * By default mono string marshaler creates .Net string for returned UTF-8 C string
 * and calls free for returned value, thus returned strings should be allocated on heap
 * @param string original C string
 */
char* MakeStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

/**
 * It is used to send callbacks to the Unity event handler
 * @param objectName name of the target GameObject
 * @param methodName name of the handler method
 * @param param message string
 */
extern void UnitySendMessage(const char* objectName, const char* methodName, const char* param);

/**
 * Name of the Gameobject in Unity that handles the Purchasing Events
 */
const char* EventHandler = "WowStore";

@interface WowPurchasing : NSObject <SKPaymentTransactionObserver, SKProductsRequestDelegate>

/**
 * Get instance of the StoreKit delegate
 * @return instance of the StoreKit delegate
 */
+ (WowPurchasing*)instance;

/**
 * Request sku listing from the AppStore
 * @param skus product IDs
 */
- (void)requestSKUs:(NSSet*)skus;

/**
 * Start async purchase process
 */
- (void)startPurchase:(NSString*)sku;

/**
 * This is required by AppStore.
 * Separate button for restoration should be added somewhere in the application
 */
- (void)restorePurchases;

@end

@implementation WowPurchasing

// Internal

/**
 * Collection of product identifiers
 */
NSSet* m_skus;

/**
 * Map of product listings
 * Information is requested from the store
 */
NSMutableArray* m_skuMap;

/**
 * Dictionary {sku: product}
 */
NSMutableDictionary* m_productMap;


- (void)storePurchase:(NSString*)transaction forSku:(NSString*)sku
{
    NSUserDefaults *standardUserDefaults = [NSUserDefaults standardUserDefaults];
    if (standardUserDefaults)
    {
        [standardUserDefaults setObject:transaction forKey:sku];
        [standardUserDefaults synchronize];
    }
    else
        NSLog(@"Couldn't access standardUserDefaults. Purchase wasn't stored.");
}


// Init

+ (WowPurchasing*)instance
{
    static WowPurchasing* instance = nil;
    if (!instance)
        instance = [[WowPurchasing alloc] init];

    return instance;
}

- (id)init
{
    self = [super init];
    [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
    return self;
}

- (void)dealloc
{
    [[SKPaymentQueue defaultQueue] removeTransactionObserver:self];
    m_skus = nil;
    m_skuMap = nil;
    m_productMap = nil;
}


// Setup

- (void)requestSKUs:(NSSet*)skus
{
    m_skus = skus;
    SKProductsRequest *request = [[SKProductsRequest alloc] initWithProductIdentifiers:skus];
    request.delegate = self;
    [request start];
}

// Setup handler

- (void)productsRequest:(SKProductsRequest*)request didReceiveResponse:(SKProductsResponse*)response
{
    m_skuMap = [[NSMutableArray alloc] init];
    m_productMap = [[NSMutableDictionary alloc] init];

    NSArray* skProducts = response.products;
    for (SKProduct * skProduct in skProducts)
    {
        // Format the price
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
        [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
        [numberFormatter setLocale:skProduct.priceLocale];
        NSString *formattedPrice = [numberFormatter stringFromNumber:skProduct.price];

        NSLocale *priceLocale = skProduct.priceLocale;
        NSString *currencyCode = [priceLocale objectForKey:NSLocaleCurrencyCode];
        NSNumber *productPrice = skProduct.price;

        // Setup sku details
        NSDictionary* skuDetails = [NSDictionary dictionaryWithObjectsAndKeys:
                                    @"product", @"itemType",
                                    skProduct.productIdentifier, @"sku",
                                    @"product", @"type",
                                    formattedPrice, @"price",
                                    currencyCode, @"currencyCode",
                                    productPrice, @"priceValue",
                                    ([skProduct.localizedTitle length] == 0) ? @"" : skProduct.localizedTitle, @"title",
                                    ([skProduct.localizedDescription length] == 0) ? @"" : skProduct.localizedDescription, @"description",
                                    @"", @"json",
                                    nil];

        NSArray* entry = [NSArray arrayWithObjects:skProduct.productIdentifier, skuDetails, nil];
        [m_skuMap addObject:entry];
        [m_productMap setObject:skProduct forKey:skProduct.productIdentifier];
    }
    
    // create dictionary of the products with key "products" and value of the skProducts array.
    NSMutableDictionary* prods = [[NSMutableDictionary alloc] init];
    [prods setObject:skProducts forKey:@"products"];
    // create json out of it, this broadcast the event along with the json payload of the prods
    NSError* error = nil;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:prods options:kNilOptions error:&error];
    NSString* message = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    UnitySendMessage(EventHandler, "OnBillingSupported", MakeStringCopy([message UTF8String]));
}

- (void)request:(SKRequest*)request didFailWithError:(NSError*)error
{
    UnitySendMessage(EventHandler, "OnBillingNotSupported", MakeStringCopy([[error localizedDescription] UTF8String]));
}


// Transactions

- (void)startPurchase:(NSString*)sku
{
    SKProduct* product = m_productMap[sku];
    SKMutablePayment *payment = [SKMutablePayment paymentWithProduct:product];
    [[SKPaymentQueue defaultQueue] addPayment:payment];
}

- (void)restorePurchases
{
    [[SKPaymentQueue defaultQueue] restoreCompletedTransactions];
}


// Transactions handler

- (void)paymentQueue:(SKPaymentQueue *)queue updatedDownloads:(NSArray *)downloads
{
    
    // Required by store protocol
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    NSString* jsonTransaction;

    for (SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchasing:
            case SKPaymentTransactionStateDeferred:
                break;

            case SKPaymentTransactionStateFailed:
                if (transaction.error == nil)
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy("Transaction failed"));
                else if (transaction.error.code == SKErrorPaymentCancelled)
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy("Transaction cancelled"));
                else
                    UnitySendMessage(EventHandler, "OnPurchaseFailed", MakeStringCopy([[transaction.error localizedDescription] UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;

            case SKPaymentTransactionStatePurchased:
                jsonTransaction = [self convertTransactionToJson:transaction storeToUserDefaults:true];
                if ([jsonTransaction  isEqual: @"error"])
                {
                    return;
                }

                UnitySendMessage(EventHandler, "OnPurchaseSucceeded", MakeStringCopy([jsonTransaction UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
        }
    }
}

- (NSString*)convertTransactionToJson: (SKPaymentTransaction*) transaction storeToUserDefaults:(bool)store
{
    NSData *receiptData;
    if (NSFoundationVersionNumber >= NSFoundationVersionNumber_iOS_7_0) {
        receiptData = [NSData dataWithContentsOfURL:[[NSBundle mainBundle] appStoreReceiptURL]];
    } else {
        receiptData = transaction.transactionReceipt;
    }

    NSString *receiptBase64 = [receiptData base64EncodedStringWithOptions:0];

    NSDictionary *requestContents = [NSDictionary dictionaryWithObjectsAndKeys:
                                     transaction.payment.productIdentifier, @"sku",
                                     transaction.transactionIdentifier, @"orderId",
                                     receiptBase64, @"receipt",
                                     nil];

    NSError *error;
    NSData *requestData = [NSJSONSerialization dataWithJSONObject:requestContents
                                                          options:0
                                                            error:&error];
    if (!requestData) {
        NSLog(@"Got an error while creating the JSON object: %@", error);
        return @"error";
    }

    NSString * jsonString = [[NSString alloc] initWithData:requestData encoding:NSUTF8StringEncoding];

    if (store) {
        [self storePurchase:jsonString forSku:transaction.payment.productIdentifier];
    }
    
    return jsonString;
}

- (void)paymentQueue:(SKPaymentQueue*)queue restoreCompletedTransactionsFailedWithError:(NSError*)error
{
    UnitySendMessage(EventHandler, "OnRestoreFailed", MakeStringCopy([[error localizedDescription] UTF8String]));
}

- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue*)queue
{
    // to do : convert transactions into json array of PurchaseHistory
    UnitySendMessage(EventHandler, "OnRestoreFinished", MakeStringCopy(""));
}

@end

/**
 * Unity to NS String conversion
 * @param c_string original C string
 */
NSString* ToString(const char* c_string)
{
    return c_string == NULL ? [NSString stringWithUTF8String:""] : [NSString stringWithUTF8String:c_string];
}

extern "C"
{
    /**
     * Native 'requestProducts' wrapper
     * @param skus product IDs
     * @param skuNumber lenght of the 'skus' array
     */
    void AppStore_requestProducts(const char* skus[], int skuNumber)
    {
        NSMutableSet *skuSet = [NSMutableSet set];
        for (int i = 0; i < skuNumber; ++i)
            [skuSet addObject: ToString(skus[i])];
        [[WowPurchasing instance] requestSKUs:skuSet];
    }
    
    /**
     * Native 'startPurchase' wrapper
     * @param sku product ID
     */
    void AppStore_startPurchase(const char* sku)
    {
        [[WowPurchasing instance] startPurchase:ToString(sku)];
    }
    
    /**
     * Native 'restorePurchases' wrapper
     */
    void AppStore_restorePurchases()
    {
        [[WowPurchasing instance] restorePurchases];
    }
}

#endif
