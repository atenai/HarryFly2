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
	[Tooltip("タイマーテキスト（数値のみ。'TIME' のラベルは別オブジェクト）")]
	[SerializeField] TextMeshProUGUI timerText;
	public TextMeshProUGUI TimerText => timerText;

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

	/// <summary>リワード広告を最後まで見たときに貰えるコイン数</summary>
	public const int Rewarded_Coin_Amount = 1000;

	void Start()
	{
		timerText.text = gameManager.TotalTime.ToString("f1");
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;

		tapText.transform.localScale = Vector3.one;
		tapTween = tapText.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetAutoKill(false).Pause();

		goalText.gameObject.SetActive(false);

		// ゲーム開始ボタンの登録を先に済ませる。
		// 広告まわりで例外が出ても、最低限プレイを開始できる状態は保つ
		gameStartButton.onClick.AddListener(() =>
		{
			openShopButton.gameObject.SetActive(false);
			gameStartButton.gameObject.SetActive(false);
			gameManager.IsPlay = true;
		});

		InitAdsRewarded();
		InitShop();
	}

	/// <summary>
	/// リワード広告ボタンの初期化。見終わったらコインを配る
	/// </summary>
	void InitAdsRewarded()
	{
		AdsRewarded rewarded = AdsManager.SingletonInstance != null ? AdsManager.SingletonInstance.AdsRewarded : null;
		if (rewarded == null)
		{
			Debug.LogWarning("AdsRewarded が取得できないため、リワード広告ボタンを無効にする");
			if (adsRewardedButton != null)
			{
				adsRewardedButton.interactable = false;
			}
			return;
		}

		adsRewardedButton.onClick.AddListener(rewarded.ShowAd);
		rewarded.OnRewarded += OnAdsRewarded;
	}

	void OnDestroy()
	{
		// AdsRewarded は DontDestroyOnLoad で生き続けるのに対し、この UI はステージごとに
		// 作り直される。解除しないとステージを進むたびに購読が積み上がり、
		// 広告1回で何倍ものコインが入ってしまう
		AdsRewarded rewarded = AdsManager.SingletonInstance != null ? AdsManager.SingletonInstance.AdsRewarded : null;
		if (rewarded != null)
		{
			rewarded.OnRewarded -= OnAdsRewarded;
		}
	}

	/// <summary>
	/// リワード広告を最後まで見たときの報酬
	/// </summary>
	void OnAdsRewarded()
	{
		gameManager.AddRewardCoin(Rewarded_Coin_Amount);
		coinText.text = gameManager.CoinCount.ToString();
		// コインが増えて買えるようになったモデルがあるので表示を更新する
		RefreshModelButtons();
		Debug.Log("リワード広告の報酬：+" + Rewarded_Coin_Amount + "コイン");
	}

	void InitShop()
	{
		Panel_Shop.SetActive(false);
		openShopButton.onClick.AddListener(OnClickShopOpen);
		closeShopButton.onClick.AddListener(OnClickShopClose);
		// 機体スロットは6個ある想定。増減してもコードを触らずに済むよう配列を回す
		for (int i = 0; i < modelButton.Length; i++)
		{
			if (modelButton[i] == null)
			{
				continue;
			}
			// ラムダがループ変数を捕まえてしまわないよう、周回ごとにコピーする
			int index = i;
			modelButton[i].onClick.AddListener(() => OnClickModel(index));
		}

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

		// 3Dモデルがまだ入っていないスロットは選ばせない。
		// 選ばせてしまうと機体が消えたまま飛ぶことになる
		if (planeController != null && planeController.HasModel(index) == false)
		{
			Debug.Log("3Dモデルが未設定のスロットなので選択できない: " + index);
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
			if (modelButton[i] == null)
			{
				continue;
			}

			// 3Dモデルが差し込まれているスロットだけ押せるようにする
			bool hasModel = planeController != null && planeController.HasModel(i);
			modelButton[i].interactable = hasModel;

			// ボタンの子にある Text (Legacy) を探してラベルを更新
			Text label = modelButton[i].GetComponentInChildren<Text>();
			if (label == null)
			{
				continue;
			}

			if (hasModel == false)
			{
				// Plane プレハブの planePrefabs にモデルを入れれば、そのまま使えるようになる
				label.text = "準備中";
				continue;
			}

			if (ShopManager.SingletonInstance == null)
			{
				continue;
			}

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

		timerText.text = gameManager.TotalTime.ToString("f1");
		coinText.text = gameManager.CoinCount.ToString();
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;
	}

	// ボタンを押したときの処理
	public void OnButtonDown()
	{
		buttonDownFlag = true;
	}

	// ボタンを離したときの処理
	public void OnButtonUp()
	{
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
			// フェード対象が無い場合でも操作不能で固まらせない
			isFade = true;
			return null;
		}

		// SetUpdate(true) で Time.timeScale に依存させない。
		// 広告表示などで timeScale が 0 になるとフェードが途中で止まり、
		// 半透明のまま操作不能になるため
		return fadeImage.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
		{
			// isFade を最初に立てる。DOTween はセーフモードでコールバック内の例外を
			// 握り潰すので、後ろに置くと代入だけ失われて永久に操作不能になる
			isFade = true;
			if (disableOnComplete)
			{
				fadeImage.gameObject.SetActive(false);
			}
			onComplete?.Invoke();
		});
	}

	/// <summary>
	/// フェードを強制的に完了させる。
	/// 何らかの理由でトゥイーンが進まなかったときに、半透明のまま操作不能で固まるのを防ぐ保険
	/// </summary>
	public void ForceFadeComplete()
	{
		if (fadeImage != null)
		{
			fadeImage.DOKill();
			Color color = fadeImage.color;
			color.a = 0f;
			fadeImage.color = color;
			fadeImage.gameObject.SetActive(false);
		}
		isFade = true;
	}

}
