using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsBanner : MonoBehaviour
{
	[SerializeField] string _androidAdUnitId = "Banner_Android";
	[SerializeField] string _iOSAdUnitId = "Banner_iOS";
	string _adUnitId = null;

	void Start()
	{
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
		_adUnitId = _androidAdUnitId;
#endif
		//バナー広告の表示・非表示ボタンをオフにする
		//_showBannerButton.interactable = false;
		//_hideBannerButton.interactable = false;

		//バナー広告の位置をセットする
		Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);

		//バナー広告のロードをする
		//_loadBannerButton.onClick.AddListener(LoadBanner);
		//バナー広告のロードボタンをオンにする
		//_loadBannerButton.interactable = true;

		//バナー広告をロードする
		LoadBanner();
	}

	//バナー広告をロードする
	public void LoadBanner()
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

		//_showBannerButton.onClick.AddListener(ShowBannerAd);
		//_hideBannerButton.onClick.AddListener(HideBannerAd);

		//バナー広告の表示・非表示ボタンをオンにする
		//_showBannerButton.interactable = true;
		//_hideBannerButton.interactable = true;
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

	void OnDestroy()
	{
		//_loadBannerButton.onClick.RemoveAllListeners();
		//_showBannerButton.onClick.RemoveAllListeners();
		//_hideBannerButton.onClick.RemoveAllListeners();
	}
}
