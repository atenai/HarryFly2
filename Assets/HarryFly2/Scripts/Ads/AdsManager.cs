using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
	private static AdsManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static AdsManager SingletonInstance => singletonInstance;

	[SerializeField] string androidGameId = "6061896";
	[SerializeField] string iOSGameId = "6061897";

	private string gameId;
	private bool testMode = false;

	[SerializeField] AdsRewarded adsRewarded;
	public AdsRewarded AdsRewarded => adsRewarded;
	[SerializeField] AdsInterstitial adsInterstitial;
	public AdsInterstitial AdsInterstitial => adsInterstitial;
	[SerializeField] AdsBanner adsBanner;

	int adsInterstitialCount = 0;
	const int Max_AdsInterstitial_Count = 3;

	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
			DontDestroyOnLoad(this.gameObject);//シーンを切り替えた時に破棄しない
		}
		else
		{
			// 重複したインスタンスは広告を初期化せずに破棄する。
			// ここで InitializeAds() まで走ると Advertisement.Initialize が二重に呼ばれ、
			// 初期化完了コールバックも二重に返ってくるため、バナーのロードが重なって
			// 「A Banner is already in use」になる。
			// ステージを先読みするたびに新しい AdsManager が生成されるので、
			// return しないと切り替えのたびに再発する
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
			return;
		}

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
		//バナー広告をロードする
		adsBanner.LoadAd();
	}

	//初期化処理が失敗した場合に実行する
	public void OnInitializationFailed(UnityAdsInitializationError error, string message)
	{
		Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
	}

	public void ShowAdsInterstitialCount()
	{
		adsInterstitialCount++;
		if (Max_AdsInterstitial_Count <= adsInterstitialCount)
		{
			adsInterstitialCount = 0;
			adsInterstitial.ShowAd();
		}
	}
}
