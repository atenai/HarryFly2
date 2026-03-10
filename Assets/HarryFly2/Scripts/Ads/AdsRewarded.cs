using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;

//リワード広告スクリプト
public class AdsRewarded : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
	[SerializeField] Button _showAdButton;//ボタンの変数を作る理由が何かあるみたいこれ
	[SerializeField] string _androidAuUnitId = "Rewarded_Android";
	[SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
	string _adUnitId = null;

	void Awake()
	{
		//iOSかAndroidかの広告IDを取得する
		//この場合はUnityのエディターもiOSかAndroidにスイッチしておく必要がある(じゃないと_adUnitIdが取得できずエラーになる)
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
		_adUnitId = _androidAuUnitId;
#endif
		//リワード広告表示ボタンをオフにする
		_showAdButton.interactable = false;
	}

	//リワード広告をロード
	public void LoadAd()
	{
		Debug.Log("Loading Ad: " + _adUnitId);
		//リワード広告をロードする
		Advertisement.Load(_adUnitId, this);
	}

	//リワード広告を正常にロードできたらボタンを追加する
	public void OnUnityAdsAdLoaded(string adUnitId)
	{
		Debug.Log("Ad Loaded: " + adUnitId);

		if (adUnitId.Equals(_adUnitId))
		{
			//ボタンをクリックしたらShowAd関数を呼び出す
			_showAdButton.onClick.AddListener(ShowAd);
			//リワード広告表示ボタンをオンにする
			_showAdButton.interactable = true;
		}
	}

	//リワード広告を表示する
	public void ShowAd()
	{
		//リワード広告表示ボタンをオフにする
		_showAdButton.interactable = false;
		//リワード広告を表示する
		Advertisement.Show(_adUnitId, this);
	}

	//リワード広告を正常に表示完了後に実行する
	public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
	{
		if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
		{
			Debug.Log("Unity Ads Rewarded Ad Completed");

			Debug.Log("<color=red>あなたはリワード広告をゲットしました。</color>");

			//リワード広告をロードする
			Advertisement.Load(_adUnitId, this);
		}
	}

	//リワード広告のロードに失敗した際に実行する
	public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
	{
		Debug.Log($"Error loading Ad Unit {adUnitId}: {error.ToString()} - {message}");
	}

	//リワード広告の表示に失敗した際に実行する
	public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
	{
		Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
	}

	//リワード広告の表示をスタートした際に実行する
	public void OnUnityAdsShowStart(string adUnitId) { }
	//リワード広告の表示をクリックした際に実行する
	public void OnUnityAdsShowClick(string adUnitId) { }

	void OnDestroy()
	{
		//ボタンリスナーを全て消す
		_showAdButton.onClick.RemoveAllListeners();
	}
}
