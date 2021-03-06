﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wowsome.Store {
  /// <summary>
  /// The store Gameobject.
  /// Inherit this class and call InitStore(List<Product> prods)
  /// 
  /// Attach it to a Gameobject and name it as 'WowStore'
  /// 
  /// it wont work if: 
  /// 1. The name of the gameObject where you attach this script to is not 'WowStore'
  /// 2. The gameObject is inactive
  ///   
  /// /// internally, all the native code for both android and ios will try to call the gameobject with name 'WowStore'
  /// via UnitySendMessage()
  /// </summary>
  public class WowStore : MonoBehaviour {
    const string GameobjectName = "WowStore";

    IStoreController m_controller = null;

    #region Observables
    public InitSuccessEv OnInitSuccessEv { get; set; }
    public InitFailEv OnInitFailEv { get; set; }
    public PurchaseFailureEv OnPurchaseFailEv { get; set; }
    public PurchaseSuccessEv OnPurchaseSuccessEv { get; set; }
    public Action OnStartRestoreEv { get; set; }
    public PurchaseRestoredEv OnPurchaseRestoredEv { get; set; }
    public RestoreFailedEv OnRestoreFailedEv { get; set; }
    #endregion

    public void InitStore(List<Product> prods) {
      // make sure the name is WowStore.
      gameObject.name = GameobjectName;

#if UNITY_ANDROID
      m_controller = new WowStore_Android();      
#elif UNITY_IOS
      m_controller = new WowStore_iOS();
#endif

      m_controller.InitStore(prods);
    }

    public void PurchaseProduct(string sku) {
      m_controller.MakePurchase(sku);
    }

    public void RestorePurchase() {
      OnStartRestoreEv?.Invoke();
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
