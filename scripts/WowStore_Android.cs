using System.Collections.Generic;
using UnityEngine;

namespace Wowsome {
  public class WowStore_Android : IStoreController {
    public class AndroidHelper {
      AndroidJavaClass m_pluginClass = null;

      public AndroidHelper() {
        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaClass m_pluginClass = new AndroidJavaClass("wowsome.co.purchasing");
      }

      public void CallMethod(string methodName, params object[] args) {
        m_pluginClass.CallStatic(methodName, args);
      }
    }

    AndroidHelper m_androidHelper = new AndroidHelper();

    #region IStoreController
    public void InitStore(List<Product> products) {
      m_androidHelper.CallMethod("initStore", "google");
      m_androidHelper.CallMethod("requestProducts", products.Map(x => x.Sku).ToArray());
    }

    public void MakePurchase(string productId) {
      m_androidHelper.CallMethod("startPurchase", productId);
    }

    public void RestorePurchase() { }
    #endregion
  }
}