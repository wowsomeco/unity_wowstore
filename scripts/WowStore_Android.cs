using System.Collections.Generic;
using UnityEngine;

namespace Wowsome {
  namespace Store {
    public class WowStore_Android : IStoreController {
      public class AndroidHelper {
        AndroidJavaClass m_pluginClass = null;

        public AndroidHelper() {
          m_pluginClass = new AndroidJavaClass("wowsome.co.purchasing.WowPurchasing");
        }

        public void CallMethod(string methodName, params object[] args) {
          m_pluginClass.CallStatic(methodName, args);
        }
      }

      AndroidHelper m_androidHelper = null;

      #region IStoreController
      public void InitStore(List<Product> products) {
        // for now it's either google or amazon
        // refactor this later accordingly should there be more impl for another stores e.g. samsung, etc.        
        m_androidHelper = new AndroidHelper();
        m_androidHelper.CallMethod("initStore", AppSettings.AndroidPlatform == AndroidPlatform.Google ? "google" : "amazon");
        string[] prodArray = products.Map(x => x.Sku).ToArray();
        m_androidHelper.CallMethod("requestProducts", (object)prodArray);
      }

      public void MakePurchase(string productId) {
        m_androidHelper.CallMethod("startPurchase", productId);
      }

      public void RestorePurchase() {
        m_androidHelper.CallMethod("restorePurchase", null);
      }
      #endregion
    }
  }
}