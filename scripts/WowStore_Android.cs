using System.Collections.Generic;
using UnityEngine;

namespace Wowsome {
  namespace Store {
    public class WowStore_Android : IStoreController {
      public class AndroidHelper {
#if !UNITY_EDITOR
        AndroidJavaClass m_pluginClass = null;
#endif

        public AndroidHelper() {
#if !UNITY_EDITOR
          AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
          AndroidJavaClass m_pluginClass = new AndroidJavaClass("wowsome.co.purchasing");
#endif
        }

        public void CallMethod(string methodName, params object[] args) {
#if !UNITY_EDITOR
          m_pluginClass.CallStatic(methodName, args);
#endif
        }
      }

      AndroidHelper m_androidHelper = new AndroidHelper();

      #region IStoreController
      public void InitStore(StoreProvider provider, List<Product> products) {
        // for now it's either google or amazon
        // refactor this later accordingly should there be more impl for another stores e.g. samsung, etc.
        m_androidHelper.CallMethod("initStore", provider == StoreProvider.Google ? "google" : "amazon");
        m_androidHelper.CallMethod("requestProducts", products.Map(x => x.Sku).ToArray());
      }

      public void MakePurchase(string productId) {
        m_androidHelper.CallMethod("startPurchase", productId);
      }

      public void RestorePurchase() { }
      #endregion
    }
  }
}