using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInterstitial : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
	[SerializeField] string _androidAdUnitId = "Interstitial_Android";
	[SerializeField] string _iOSAdUnitId = "Interstitial_iOS";
	string _adUnitId;

	void Awake()
	{
		//iOSかAndroidのどちらのプラットフォームかを取得して広告IDを取得する
		_adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer) ? _iOSAdUnitId : _androidAdUnitId;
	}

	//インターステーショナル広告をロードする
	public void LoadAd()
	{
		Debug.Log("Loading Ad: " + _adUnitId);
		//インターステーショナル広告をロード
		Advertisement.Load(_adUnitId, this);
	}

	//インターステーショナル広告を表示する
	public void ShowAd()
	{
		Debug.Log("Showing Ad: " + _adUnitId);
		//インターステーショナル広告を表示
		Advertisement.Show(_adUnitId, this);
	}

	//インターステーショナル広告を正常にロードできた場合に実行する
	public void OnUnityAdsAdLoaded(string adUnitId)
	{

	}

	//インターステーショナル広告のロードに失敗した場合に実行する
	public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
	{
		Debug.Log($"Error loading Ad Unit: {adUnitId} - {error.ToString()} - {message}");
	}

	//インターステーショナル広告の表示に失敗した場合に実行する
	public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
	{
		Debug.Log($"Error showing Ad Unit: {adUnitId} - {error.ToString()} - {message}");
	}

	public void OnUnityAdsShowStart(string adUnitId) { }
	public void OnUnityAdsShowClick(string adUnitId) { }

	//インターステーショナル広告を正常に表示完了後に実行する
	public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
	{
		Debug.Log("<color=blue>あなたはインターステーショナル広告をゲットしました。</color>");

		//インターステーショナル広告をロード
		Advertisement.Load(_adUnitId, this);
	}
}
