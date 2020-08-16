using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wowsome {
  namespace Store {
    public class WowStore_iOS : IStoreController {
      [DllImport("__Internal")]
      private static extern void AppStore_requestProducts(string[] skus, int skusNumber);

      [DllImport("__Internal")]
      private static extern void AppStore_startPurchase(string sku);

      [DllImport("__Internal")]
      private static extern void AppStore_restorePurchases();

      #region IStoreController
      public void InitStore(StoreProvider provider, List<Product> products) {
        AppStore_requestProducts(products.Map(x => x.Sku).ToArray(), products.Count);
      }

      public void MakePurchase(string productId) {
        AppStore_startPurchase(productId);
      }

      public void RestorePurchase() {
        AppStore_restorePurchases();
      }
      #endregion
    }
  }
}