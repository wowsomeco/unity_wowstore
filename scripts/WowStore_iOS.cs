namespace Wowsome {
  public class WowStore_iOS {
#if UNITY_IOS
    #region NativeMethods
        [DllImport("__Internal")]
        private static extern void AppStore_requestProducts(string[] skus, int skusNumber);

        [DllImport("__Internal")]
        private static extern void AppStore_startPurchase(string sku);

        [DllImport("__Internal")]
        private static extern void AppStore_restorePurchases();

        [DllImport("__Internal")]
        private static extern bool Inventory_hasPurchase(string sku);

        [DllImport("__Internal")]
        private static extern void Inventory_query();

        [DllImport("__Internal")]
        private static extern void Inventory_removePurchase(string sku);
    #endregion
#endif
  }
}