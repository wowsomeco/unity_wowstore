using System.Collections.Generic;
using UnityEngine;

namespace Wowsome {
  public class WowStore : MonoBehaviour {
    IStoreController m_controller = null;

    #region Observables
    public InitSuccessEv OnInitSuccessEv { get; set; }
    public InitFailEv OnInitFailEv { get; set; }
    public PurchaseFailureEv OnPurchaseFailEv { get; set; }
    public PurchaseSuccessEv OnPurchaseSuccessEv { get; set; }
    public PurchaseRestoredEv OnPurchaseRestoredEv { get; set; }
    public RestoreFailedEv OnRestoreFailedEv { get; set; }
    #endregion

    public void InitStore(List<Product> prods) {
#if UNITY_ANDROID
      m_controller = new WowStore_Android();
#elif UNITY_IOS
      m_controller = new WowStore_iOS();
#endif

      if (null != m_controller) {
        m_controller.InitStore(prods);
      }
    }

    public void PurchaseProduct(string sku) {
      m_controller.MakePurchase(sku);
    }

    public void RestorePurchase() {
      m_controller.RestorePurchase();
    }

    void OnBillingSupported(string json) {
      AvailableProduct prods = JsonUtility.FromJson<AvailableProduct>(json);
      OnInitSuccessEv?.Invoke(prods);
    }

    void OnBillingNotSupported(string error) {
      OnInitFailEv?.Invoke(error);
    }

    void OnPurchaseSucceeded(string json) {
      StoreReceipt r = JsonUtility.FromJson<StoreReceipt>(json);
      OnPurchaseSuccessEv?.Invoke(r);
    }

    void OnPurchaseFailed(string message) {
      OnPurchaseFailEv?.Invoke(message);
    }

    #region Restore Purchase
    void OnPurchaseRestored(string json) {
      PurchaseHistory h = JsonUtility.FromJson<PurchaseHistory>(json);
      OnPurchaseRestoredEv?.Invoke(h);
    }

    void OnRestoreFailed(string error) {
      OnRestoreFailedEv?.Invoke(error);
    }
    #endregion
  }
}
