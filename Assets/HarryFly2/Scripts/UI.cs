using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// UI
/// </summary>
public class UI : MonoBehaviour
{
	[Tooltip("飛行機のモデル")]
	[SerializeField] PlaneController planeController;
	[Tooltip("ゲームマネージャー")]
	[SerializeField] GameManager gameManager;

	[Tooltip("ショップパネル")]
	[SerializeField] GameObject panel_Shop;
	public GameObject Panel_Shop => panel_Shop;
	[Tooltip("ショップを開くボタン")]
	[SerializeField] Button openShopButton;
	[Tooltip("ショップを閉じるボタン")]
	[SerializeField] Button closeShopButton;
	[Tooltip("飛行機のモデル")]
	[SerializeField] Button[] modelButton;

	[Tooltip("ゲームスタートボタン")]
	[SerializeField] Button gameStartButton;

	[Header("UIに関する変数")]
	[Tooltip("タイマーテキスト")]
	[SerializeField] Text timerText;
	public Text TimerText => timerText;

	[Tooltip("燃料スライダー")]
	[SerializeField] Slider fuelSlider;

	[Tooltip("ジョイスティック")]
	[SerializeField] FloatingJoystick floatingJoystick;
	public FloatingJoystick FloatingJoystick => floatingJoystick;

	[Tooltip("加速ボタン")]
	[SerializeField] Button accelerateButton;
	public Button AccelerateButton => accelerateButton;

	// ボタンを押したときtrue、離したときfalseになるフラグ
	bool buttonDownFlag = false;
	public bool ButtonDownFlag => buttonDownFlag;

	[Tooltip("タップテキスト")]
	[SerializeField] TextMeshProUGUI tapText;
	private Tween tapTween;

	[Header("Fade")]
	[Tooltip("フェード対象の Image (任意)")]
	[SerializeField] Image fadeImage;
	bool isFade = false;
	public bool IsFade => isFade;

	[Tooltip("コインテキスト")]
	[SerializeField] TextMeshProUGUI coinText;
	public TextMeshProUGUI CoinText => coinText;

	[Tooltip("ゴール時に中央に出すテキスト")]
	[SerializeField] TextMeshProUGUI goalText;

	[Tooltip("広告報酬ボタン")]
	[SerializeField] Button adsRewardedButton;

	void Start()
	{
		timerText.text = "残り時間：" + gameManager.TotalTime.ToString("f1");
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;

		tapText.transform.localScale = Vector3.one;
		tapTween = tapText.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetAutoKill(false).Pause();

		if (goalText != null)
		{
			goalText.gameObject.SetActive(false);
		}

		adsRewardedButton.onClick.AddListener(AdsManager.SingletonInstance.AdsRewarded.ShowAd);
		gameStartButton.onClick.AddListener(() =>
		{
			openShopButton.gameObject.SetActive(false);
			gameStartButton.gameObject.SetActive(false);
			gameManager.IsPlay = true;
		});

		InitShop();
	}

	void InitShop()
	{
		Panel_Shop.SetActive(false);
		openShopButton.onClick.AddListener(OnClickShopOpen);
		closeShopButton.onClick.AddListener(OnClickShopClose);
		modelButton[0].onClick.AddListener(() => OnClickModel(0));
		modelButton[1].onClick.AddListener(() => OnClickModel(1));

		RefreshModelButtons();
	}

	void OnClickShopOpen()
	{
		Panel_Shop.SetActive(true);
		gameStartButton.gameObject.SetActive(false);
	}

	void OnClickShopClose()
	{
		Panel_Shop.SetActive(false);
		gameStartButton.gameObject.SetActive(true);
	}


	void OnClickModel(int index)
	{
		if (ShopManager.SingletonInstance == null)
		{
			Debug.LogWarning("ShopManager not found");
			return;
		}

		bool result = ShopManager.SingletonInstance.SelectModel(index);
		if (result)
		{
			// 選択/購入成功したらボタン状態を更新
			RefreshModelButtons();
		}
	}

	void RefreshModelButtons()
	{
		if (modelButton == null || modelButton.Length == 0) return;
		for (int i = 0; i < modelButton.Length; i++)
		{
			modelButton[i].interactable = true; // 押せるようにしておく

			// ボタンの子にある Text (Legacy) を探してラベルを更新
			Text label = modelButton[i].GetComponentInChildren<Text>();
			if (label != null)
			{
				if (ShopManager.SingletonInstance != null)
				{
					bool unlocked = ShopManager.SingletonInstance.IsUnlocked(i);
					int price = ShopManager.SingletonInstance.GetPrice(i);
					if (!unlocked)
					{
						if (price >= 0)
							label.text = "ロック\n必要:" + price.ToString() + "コイン";
						else
							label.text = "ロック";
					}
					else
					{
						if (ShopManager.SingletonInstance.PlaneModelNumber == i)
							label.text = "選択中";
						else
							label.text = "使用可能";
					}
				}
			}
		}
	}

	void Update()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		if (isFade == false)
		{
			return;
		}

		if (gameManager.IsPlay == false)
		{
			tapText.gameObject.SetActive(true);
			tapTween.Play();
			return;
		}
		else
		{
			tapTween.Pause();
			tapText.gameObject.SetActive(false);
			tapText.transform.localScale = Vector3.one;
		}

		timerText.text = "残り時間：" + gameManager.TotalTime.ToString("f1");
		coinText.text = gameManager.CoinCount.ToString();
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;
	}

	// ボタンを押したときの処理
	public void OnButtonDown()
	{
		Debug.Log("Down");
		buttonDownFlag = true;
	}

	// ボタンを離したときの処理
	public void OnButtonUp()
	{
		Debug.Log("Up");
		buttonDownFlag = false;
	}

	/// <summary>
	/// ゴール時に「GOAL!!」を画面中央に表示します。
	/// フェード用の Image より手前に配置してあるので、暗転した上に重なって出ます。
	/// </summary>
	public void ShowGoalText()
	{
		goalText.gameObject.SetActive(true);
		// 小さい状態から弾むように出す
		goalText.transform.localScale = Vector3.one * 0.5f;
		goalText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
	}

	/// <summary>
	/// 指定した Image をフェードインします。
	/// 不透明にする
	/// </summary>
	public void FadeIn()
	{
		fadeImage.gameObject.SetActive(true);
		Color color = fadeImage.color;
		color.a = 1f;
		fadeImage.color = color;
	}

	/// <summary>
	/// 指定した Image をフェードアウトします。
	/// 透明にする
	/// </summary>
	public Tween FadeOut(float duration = 1f, bool disableOnComplete = true, TweenCallback onComplete = null)
	{
		if (fadeImage == null)
		{
			return null;
		}

		return fadeImage.DOFade(0f, duration).OnComplete(() =>
		{
			if (disableOnComplete)
			{
				fadeImage.gameObject.SetActive(false);
			}
			isFade = true;
			onComplete?.Invoke();
		});
	}

}
