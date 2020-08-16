using System.Collections.Generic;

namespace Wowsome {
  public class WowStore_iOS : IStoreController {
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void AppStore_requestProducts(string[] skus, int skusNumber);

    [DllImport("__Internal")]
    private static extern void AppStore_startPurchase(string sku);

    [DllImport("__Internal")]
    private static extern void AppStore_restorePurchases();    
#endif

    #region IStoreController
    public void InitStore(List<Product> products) {
#if UNITY_IOS
      AppStore_requestProducts(products.Map(x => x.Id).ToArray(), products.Count);
#endif
    }

    public void MakePurchase(string productId) {
#if UNITY_IOS
      AppStore_startPurchase(productId);
#endif
    }

    public void RestorePurchase() {
#if UNITY_IOS
      AppStore_restorePurchases();
#endif
    }
    #endregion
  }
}