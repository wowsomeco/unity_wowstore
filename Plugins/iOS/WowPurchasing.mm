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
const char* BillingSupportedEv = "OnBillingSupported";
const char* BillingNotSupportedEv = "OnBillingNotSupported";
const char* PurchaseSucceededEv = "OnPurchaseSucceeded";
const char* PurchaseFailedEv = "OnPurchaseFailed";
const char* PurchaseRestoredEv = "OnPurchaseRestored";
const char* RestoreFailedEv = "OnRestoreFailed";

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
 * Dictionary {sku: product}
 */
NSMutableDictionary* m_productMap;

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
    m_productMap = [[NSMutableDictionary alloc] init];
    
    NSMutableArray* productDetails = [[NSMutableArray alloc] init];

    NSArray* skProducts = response.products;
    for (SKProduct * skProduct in skProducts)
    {
        // Format the price
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
        [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
        [numberFormatter setLocale:skProduct.priceLocale];
        NSString *formattedPrice = [numberFormatter stringFromNumber:skProduct.price];
        // NSLocale *priceLocale = skProduct.priceLocale;
        // NSString *currencyCode = [priceLocale objectForKey:NSLocaleCurrencyCode];
        // NSNumber *productPrice = skProduct.price;

        // Setup sku details
        NSDictionary* skuDetails = [NSDictionary dictionaryWithObjectsAndKeys:
                                    @"product", @"itemType",
                                    skProduct.productIdentifier, @"sku",
                                    formattedPrice, @"price",
                                    ([skProduct.localizedTitle length] == 0) ? @"" : skProduct.localizedTitle, @"title",
                                    ([skProduct.localizedDescription length] == 0) ? @"" : skProduct.localizedDescription, @"description",
                                    nil];
                
        [m_productMap setObject:skProduct forKey:skProduct.productIdentifier];
        [productDetails addObject:skuDetails];
    }
    
    // create dictionary of the products with key "products" and value of the skProducts array.
    NSMutableDictionary* prods = [[NSMutableDictionary alloc] init];
    [prods setObject:productDetails forKey:@"products"];
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
    for (SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchasing:
            case SKPaymentTransactionStateDeferred:
                break;
            case SKPaymentTransactionStateFailed:
                if (transaction.error == nil)
                    UnitySendMessage(EventHandler, PurchaseFailedEv, MakeStringCopy("Transaction failed"));
                else if (transaction.error.code == SKErrorPaymentCancelled)
                    UnitySendMessage(EventHandler, PurchaseFailedEv, MakeStringCopy("Transaction cancelled"));
                else
                    UnitySendMessage(EventHandler, PurchaseFailedEv, MakeStringCopy([[transaction.error localizedDescription] UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStateRestored:
                UnitySendMessage(EventHandler, PurchaseSucceededEv, MakeStringCopy([[self toStoreReceiptString:transaction] UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStatePurchased:
                UnitySendMessage(EventHandler, PurchaseSucceededEv, MakeStringCopy([[self toStoreReceiptString:transaction] UTF8String]));
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
        }
    }
}

- (void)paymentQueue:(SKPaymentQueue*)queue restoreCompletedTransactionsFailedWithError:(NSError*)error
{
    UnitySendMessage(EventHandler, RestoreFailedEv, MakeStringCopy([[error localizedDescription] UTF8String]));
}


- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue*)queue
{
    // convert all the transaction array and broadcast to unity
    NSMutableArray* receipts = [[NSMutableArray alloc] init];
    for (SKPaymentTransaction* transaction in queue.transactions)
    {
        [receipts addObject:[self toStoreReceipt:transaction]];
    }
        
    NSMutableDictionary* receiptModel = [[NSMutableDictionary alloc] init];
    [receiptModel setObject:receipts forKey:@"purchased"];
    // create json out of it
    NSError* error = nil;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:receiptModel options:kNilOptions error:&error];
    
    if (!jsonData) {
        NSLog(@"Got an error while creating the JSON object: %@", error);
    }
    
    NSString* message = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    UnitySendMessage(EventHandler, PurchaseRestoredEv, MakeStringCopy([message UTF8String]));
    
}

- (NSDictionary*) toStoreReceipt:(SKPaymentTransaction*) transaction
{
    NSDictionary *receipt = [NSDictionary dictionaryWithObjectsAndKeys:
                                     transaction.payment.productIdentifier, @"sku",
                                     @"product", @"itemType",
                                     nil];
    
    return receipt;
}

-(NSString*) toStoreReceiptString:(SKPaymentTransaction*) transaction
{
    NSDictionary* dict = [self toStoreReceipt:transaction];
    NSError* error;
    NSData* requestData = [NSJSONSerialization dataWithJSONObject:dict
                                                          options:0
                                                            error:&error];
    if (!requestData) {
        NSLog(@"Got an error while creating the JSON object: %@", error);
        return @"error";
    }
    
    NSString* jsonString = [[NSString alloc] initWithData:requestData encoding:NSUTF8StringEncoding];
    return jsonString;
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
