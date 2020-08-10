using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Wowsome {
  namespace Monetization {
    public class StoreManager : MonoBehaviour, IStoreListener {
      #region Vars
      [Serializable]
      public struct StoreProductData {
        public string Id;
        public ProductType Type;

        public StoreProductData(string theId, ProductType theType) {
          Id = theId;
          Type = theType;
        }

        public StoreProductData(string theId) : this(theId, ProductType.Consumable) { }
      }

      public enum PurchasingStatus {
        Purchasing, NotFoundId, NotInitialized
      }

      public enum RestorePurchaseStatus {
        Start, Failed, Continue, Success, UnsupportedPlatform
      }

      public delegate void InitFailEv(InitializationFailureReason reason);
      public delegate void PurchaseSuccessEv(string purchaseId, Product prod);
      public delegate void PurchaseFailureEv(PurchaseFailureReason reason);
      public delegate void PurchasingEv(PurchasingStatus status);
      public delegate void RestoreEv(RestorePurchaseStatus status);
      #endregion

      Product[] m_prods;

      static IStoreController m_storeController;          // The Unity Purchasing system.
      static IExtensionProvider m_storeExtensionProvider; // The store-specific Purchasing subsystems.

      public Dictionary<string, StoreProductData> StoreProducts { get; private set; }

      public Product[] Prods {
        get { return m_prods; }
        set { m_prods = value; }
      }

      #region Observables
      public InitFailEv OnInitFailEv { get; set; }
      public PurchaseFailureEv OnPurchaseFailEv { get; set; }
      public PurchaseSuccessEv OnPurchaseSuccessEv { get; set; }
      public PurchasingEv OnPurchasingEv { get; set; }
      public RestoreEv OnRestoreEv { get; set; }
      #endregion      

      public bool HasInitialized {
        get {
          // Only say we are initialized if both the Purchasing references are set.
          return m_storeController != null && m_storeExtensionProvider != null;
        }
      }

      public StoreManager(IEnumerable<StoreProductData> storeProducts) {
        //dont process if the store has been initialized previously
        if (HasInitialized) return;
        //init the product datas
        StoreProducts = new Dictionary<string, StoreProductData>();
        foreach (StoreProductData prodData in storeProducts) {
          StoreProducts.Add(prodData.Id, prodData);
        }
        // init store        
        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (KeyValuePair<string, StoreProductData> storeData in StoreProducts) {
          builder.AddProduct(storeData.Key, storeData.Value.Type);
        }

        UnityPurchasing.Initialize(this, builder);
      }

      public void PurchaseProduct(string productId) {
        Debug.Log("trying to find product id : " + productId);
        if (StoreProducts.ContainsKey(productId)) {
          Debug.Log("id found , make purchase : " + productId);
          // If Purchasing has been initialized ...
          if (HasInitialized) {
            Product product = m_storeController.products.WithID(productId);
            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase) {
              OnPurchasingEv?.Invoke(PurchasingStatus.Purchasing);
              m_storeController.InitiatePurchase(product);
            }
            // Otherwise ...
            else {
              OnPurchasingEv?.Invoke(PurchasingStatus.NotFoundId);
            }
          }
          // Otherwise ...
          else {
            OnPurchasingEv?.Invoke(PurchasingStatus.NotInitialized);
          }
        }
      }

      public void RestorePurchase() {
        // If Purchasing has not yet been set up ...
        if (!HasInitialized) {
          // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
          OnPurchasingEv?.Invoke(PurchasingStatus.NotInitialized);
          return;
        }

        // If we are running on an Apple device ... 
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer) {
          // ... begin restoring purchases
          Debug.Log("RestorePurchases started ...");
          OnRestoreEv?.Invoke(RestorePurchaseStatus.Start);

          // Fetch the Apple store-specific subsystem.
          var apple = m_storeExtensionProvider.GetExtension<IAppleExtensions>();
          // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
          // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
          apple.RestoreTransactions((result) => {
            // The first phase of restoration. If no more responses are received on ProcessPurchase then 
            // no purchases are available to be restored.            
            OnRestoreEv?.Invoke(result ? RestorePurchaseStatus.Success : RestorePurchaseStatus.Failed);
            Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
          });
        }
        // Otherwise ...
        else {
          OnRestoreEv?.Invoke(RestorePurchaseStatus.UnsupportedPlatform);
        }
      }

      #region IStoreListener
      public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
        // Overall Purchasing system, configured with products for this application.
        m_storeController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        m_storeExtensionProvider = extensions;
        // store the products
        Prods = m_storeController.products.all;
      }

      public void OnInitializeFailed(InitializationFailureReason error) {
        OnInitFailEv?.Invoke(error);
      }

      public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
        Product prod = StoreProducts.ContainsKey(args.purchasedProduct.definition.id) ? args.purchasedProduct : null;
        string id = args.purchasedProduct.definition.id;
        // broadcast the purchase event        
        OnPurchaseSuccessEv?.Invoke(id, prod);
        // Return a flag indicating whether this product has completely been received, or if the application needs 
        // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
        // saving purchased products to the cloud, and when that save is delayed. 
        return PurchaseProcessingResult.Complete;
      }

      public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
        // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        // this reason with the user to guide their troubleshooting actions.        
        OnPurchaseFailEv?.Invoke(failureReason);
      }
      #endregion
    }
  }
}

