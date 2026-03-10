using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
	[SerializeField] string androidGameId = "6061896";
	[SerializeField] string iOSGameId = "6061897";

	private string gameId;
	private bool testMode = false;

	[SerializeField] AdsRewarded adsRewarded;
	[SerializeField] AdsInterstitial adsInterstitial;

	void Awake()
	{
		InitializeAds();
	}

	//広告の初期化処理
	public void InitializeAds()
	{
		//iOSかAndroidのどちらのプラットフォームかを取得して広告IDを取得する
		gameId = (Application.platform == RuntimePlatform.IPhonePlayer) ? iOSGameId : androidGameId;
		//広告の初期化処理(第一引数に広告ID, 第二引数にテストモードかどうか?, 第三引数はわからない)
		Advertisement.Initialize(gameId, testMode, this);
	}

	//初期化処理が完了した際に実行する
	public void OnInitializationComplete()
	{
		Debug.Log("Unity Ads initialization complete");
		//リワード広告をロードする
		adsRewarded.LoadAd();
		//インターステーショナル広告をロードする
		adsInterstitial.LoadAd();
	}

	//初期化処理が失敗した場合に実行する
	public void OnInitializationFailed(UnityAdsInitializationError error, string message)
	{
		Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
	}
}
