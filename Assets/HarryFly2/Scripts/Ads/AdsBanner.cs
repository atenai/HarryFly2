using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsBanner : MonoBehaviour
{
	[SerializeField] string _androidAdUnitId = "Banner_Android";
	[SerializeField] string _iOSAdUnitId = "Banner_iOS";
	string _adUnitId = null;

	void Awake()
	{
		//iOSかAndroidのどちらのプラットフォームかを取得して広告IDを取得する
		_adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer) ? _iOSAdUnitId : _androidAdUnitId;

		//バナー広告の位置をセットする
		Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
	}

	//バナー広告をロードする
	public void LoadAd()
	{
		Debug.Log("Loading Ad: " + _adUnitId);
		LoadBanner();
	}

	//バナー広告をロードする
	void LoadBanner()
	{
		BannerLoadOptions options = new BannerLoadOptions
		{
			loadCallback = OnBannerLoaded,
			errorCallback = OnBannerError
		};

		//バナー広告をロードする
		Advertisement.Banner.Load(_adUnitId, options);
	}

	//バナー広告のロードを完了した際に実行する
	void OnBannerLoaded()
	{
		Debug.Log("Banner loaded");
		//バナー広告を表示する
		ShowBannerAd();
	}

	//バナー広告がエラーの場合に実行する
	void OnBannerError(string message)
	{
		Debug.Log($"Banner Error: {message}");
	}

	//バナー広告を表示する
	void ShowBannerAd()
	{
		BannerOptions options = new BannerOptions
		{
			clickCallback = OnBannerClicked,
			hideCallback = OnBannerHidden,
			showCallback = OnBannerShown
		};

		//バナー広告を表示する
		Advertisement.Banner.Show(_adUnitId, options);
	}

	//バナー広告を非表示にする
	void HideBannerAd()
	{
		//バナー広告を非表示にする
		Advertisement.Banner.Hide();
	}

	void OnBannerClicked() { }
	void OnBannerShown() { }
	void OnBannerHidden() { }
}
