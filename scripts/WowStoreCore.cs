using System;
using System.Collections.Generic;

namespace Wowsome {
  [Serializable]
  public enum ProductType {
    NonConsumable,
    Consumable,
  }

  [Serializable]
  public class Product {
    /// <summary>
    /// Product SKU
    /// </summary>
    public string Id;
    public ProductType Type;

    public Product(string theId, ProductType theType) {
      Id = theId;
      Type = theType;
    }

    public Product(string theId) : this(theId, ProductType.NonConsumable) { }
  }

  [Serializable]
  public class StoreProduct {
    public string itemType;
    public string sku;
    public string title;
    public string description;
    public string price;
  }

  [Serializable]
  public class StoreReceipt {
    public string sku;
    public string itemType;
  }

  [Serializable]
  public class AvailableProduct {
    public List<StoreProduct> products;
  }

  [Serializable]
  public class PurchaseHistory {
    public List<StoreReceipt> purchased;
  }

  public delegate void InitFailEv(string error);
  public delegate void InitSuccessEv(AvailableProduct products);
  public delegate void PurchaseSuccessEv(StoreReceipt purchase);
  public delegate void PurchaseFailureEv(string error);
  public delegate void PurchaseRestoredEv(PurchaseHistory purchased);
  public delegate void RestoreFailedEv(string error);

  public interface IStoreController {
    void InitStore(List<Product> products);
    void RestorePurchase();
    void MakePurchase(string productId);
  }
}