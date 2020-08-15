using System;

namespace Wowsome {
  [Serializable]
  public enum ProductType {
    NonConsumable,
    Consumable,
    Subscription
  }

  [Serializable]
  public struct Product {
    public string Id;
    public ProductType Type;

    public Product(string theId, ProductType theType) {
      Id = theId;
      Type = theType;
    }

    public Product(string theId) : this(theId, ProductType.Consumable) { }
  }

  public enum PurchasingStatus {
    Purchasing, NotFoundId, NotInitialized
  }

  public enum RestorePurchaseStatus {
    Start, Failed, Continue, Success, UnsupportedPlatform
  }

  public enum PurchaseFailureReason {
    Failed, Cancelled
  }

  //   public delegate void InitFailEv(InitializationFailureReason reason);
  public delegate void PurchaseSuccessEv(string purchaseId, Product prod);
  public delegate void PurchaseFailureEv(PurchaseFailureReason reason);
  public delegate void PurchasingEv(PurchasingStatus status);
  public delegate void RestoreEv(RestorePurchaseStatus status);

  public interface IStoreController {
    void InitStore();
    // void mapSku(string sku, string storeName, string storeSku);
    // void unbindService();
    // bool areSubscriptionsSupported();
    // void queryInventory();
    // void queryInventory(string[] inAppSkus);
    // void purchaseProduct(string sku, string developerPayload = "");
    // void purchaseSubscription(string sku, string developerPayload = "");
    // void consumeProduct(Purchase purchase);
    // void restoreTransactions();
  }
}