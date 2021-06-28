using System.Collections.Generic;
using UnityEngine;

namespace Wowsome {
  namespace Store {
    public class WowStore_Android : IStoreController {
      public class AndroidHelper {
        AndroidJavaClass _pluginClass = null;

        public AndroidHelper() {
          _pluginClass = new AndroidJavaClass("wowsome.co.purchasing.WowPurchasing");
        }

        public void CallMethod(string methodName, params object[] args) {
          _pluginClass.CallStatic(methodName, args);
        }
      }

      AndroidHelper _androidHelper = null;

      #region IStoreController
      public void InitStore(List<Product> products) {
        // for now it's either google or amazon
        // refactor this later accordingly should there be more impl for another stores e.g. samsung, etc.        
        _androidHelper = new AndroidHelper();
        _androidHelper.CallMethod("initStore", AppSettings.AndroidPlatform == AndroidPlatform.Google ? "google" : "amazon");
        string[] prodArray = products.Map(x => AppSettings.AndroidPlatform == AndroidPlatform.Google ? x.skuGoogle : x.skuAmazon).ToArray();
        _androidHelper.CallMethod("requestProducts", (object)prodArray);
      }

      public void MakePurchase(string productId) {
        _androidHelper.CallMethod("startPurchase", productId);
      }

      public void RestorePurchase() {
        _androidHelper.CallMethod("restorePurchase", null);
      }
      #endregion
    }
  }
}