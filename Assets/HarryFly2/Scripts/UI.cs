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

	[Tooltip("燃料バーの残像。減った量が遅れて追いかけてくる")]
	[SerializeField] Image fuelGhostFill;

	// 危険域の演出用
	static readonly Color Hud_Bone = new Color32(0xE6, 0xEA, 0xEC, 0xFF);
	static readonly Color Hud_Orange = new Color32(0xFF, 0x7A, 0x1A, 0xFF);
	static readonly Color Hud_Critical = new Color32(0xE0, 0x3B, 0x36, 0xFF);

	/// <summary>この割合を下回ったら燃料バーを赤にする</summary>
	const float Fuel_Critical_Ratio = 0.25f;
	/// <summary>この秒数を切ったらタイマーを赤にする</summary>
	const float Timer_Critical_Seconds = 5f;
	/// <summary>
	/// 残像が本体に追いつく速さ（1秒あたりの割合）。
	/// 燃料は毎秒0.6の割合で減るので、それより遅くしないと残像が遅れず見えない
	/// </summary>
	const float Ghost_Catchup_Per_Second = 0.28f;

	/// <summary>燃料バーの塗り。Slider から取り出して色を変えるのに使う</summary>
	Image fuelFillImage;
	/// <summary>残像が示している割合</summary>
	float fuelGhostRatio = 1f;

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

	[Header("リザルト")]
	[Tooltip("リザルトパネル")]
	[SerializeField] GameObject panel_Result;
	[Tooltip("クリアタイムの数値")]
	[SerializeField] TextMeshProUGUI resultTimeText;
	[Tooltip("このステージで取得したコインの数値（ボーナス込み）")]
	[SerializeField] TextMeshProUGUI resultCoinText;
	[Tooltip("ゴールボーナスの内訳")]
	[SerializeField] TextMeshProUGUI resultBonusText;
	[Tooltip("次のステージへ進むボタン")]
	[SerializeField] Button nextStageButton;

	/// <summary>リザルトを表示中かどうか</summary>
	bool isResultShown = false;
	public bool IsResultShown => isResultShown;

	/// <summary>ゴール文字をリザルト表示時に移動させる高さ</summary>
	const float Goal_Text_Result_PosY = 470f;

	/// <summary>紙吹雪を出す親。UI の一番手前に実行時に作る</summary>
	RectTransform confettiRoot;

	/// <summary>
	/// 降っている紙吹雪1枚分の状態。
	/// 枚数を増やしたいので、DOTween ではなく毎フレームの計算で動かしている。
	/// 1枚につきトゥイーンを3つ積むと、既定の同時実行数（Tweener 200 / Sequence 50）を
	/// すぐ超えて、DOTween が内部配列を作り直すためゴールの瞬間に引っかかる
	/// </summary>
	class ConfettiPiece
	{
		public RectTransform Rect;
		/// <summary>左右に揺れる中心のX座標</summary>
		public float BaseX;
		public float PositionY;
		/// <summary>落下速度（1秒あたり）</summary>
		public float FallSpeed;
		/// <summary>左右の振れ幅</summary>
		public float SwayAmplitude;
		/// <summary>左右に揺れる速さ</summary>
		public float SwayFrequency;
		/// <summary>揺れの位相。全部の紙が同じ動きに揃わないようにする</summary>
		public float SwayPhase;
		/// <summary>回転速度（1秒あたりの角度）</summary>
		public float SpinSpeed;
		public float Angle;
	}

	/// <summary>出している紙吹雪</summary>
	readonly List<ConfettiPiece> confettiPieces = new List<ConfettiPiece>();

	/// <summary>この高さより下まで落ちた紙吹雪は消す</summary>
	float confettiRemoveY = 0f;

	/// <summary>1回のゴールで出す紙吹雪の枚数</summary>
	const int Confetti_Count = 160;

	/// <summary>紙吹雪の色。HUDと同じ配色で揃える</summary>
	static readonly Color[] Confetti_Colors =
	{
		new Color32(0xFF, 0x7A, 0x1A, 0xFF),
		new Color32(0xFF, 0xB0, 0x3A, 0xFF),
		new Color32(0xE6, 0xEA, 0xEC, 0xFF),
		new Color32(0x4F, 0xC3, 0xF7, 0xFF),
	};

	[Tooltip("広告報酬ボタン")]
	[SerializeField] Button adsRewardedButton;

	/// <summary>リワード広告を最後まで見たときに貰えるコイン数</summary>
	public const int Rewarded_Coin_Amount = 1000;

	// ショップの機体スロットの状態色。
	// オレンジは「今それが選ばれている」ことだけに使い、乱用しない
	static readonly Color Shop_Label_Equipped = new Color32(0xFF, 0x7A, 0x1A, 0xFF);
	static readonly Color Shop_Label_Available = new Color32(0xE6, 0xEA, 0xEC, 0xFF);
	static readonly Color Shop_Label_Locked = new Color32(0x6E, 0x7A, 0x82, 0xFF);
	static readonly Color Shop_Label_Disabled = new Color32(0x45, 0x4E, 0x54, 0xFF);

	void Start()
	{
		timerText.text = gameManager.TotalTime.ToString("f1");
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;

		// 色を変えるために Slider の塗りを取り出しておく
		if (fuelSlider.fillRect != null)
		{
			fuelFillImage = fuelSlider.fillRect.GetComponent<Image>();
		}
		fuelGhostRatio = planeController.CurrentFuel / PlaneController.Max_Fuel;

		tapText.transform.localScale = Vector3.one;
		tapTween = tapText.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetAutoKill(false).Pause();

		goalText.gameObject.SetActive(false);
		if (panel_Result != null)
		{
			panel_Result.SetActive(false);
		}

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

		// シーンが切り替わると Canvas ごと消える。
		// ここで Destroy を呼ぶとシーン破棄中の操作になるので、参照を手放すだけにする
		confettiPieces.Clear();

		// 待機中のリザルト演出は止める。
		// 残しておくと破棄済みの RectTransform を触って DOTween が警告を出す
		if (goalText != null)
		{
			goalText.rectTransform.DOKill();
		}
		if (panel_Result != null)
		{
			panel_Result.transform.DOKill();
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

			// ボタンの子にあるラベルを更新する。
			// 表示はコンデンス体（Oswald）に日本語グリフが無いため英字の大文字で統一している
			TextMeshProUGUI label = modelButton[i].GetComponentInChildren<TextMeshProUGUI>();
			if (label == null)
			{
				continue;
			}

			if (hasModel == false)
			{
				// Plane プレハブの planePrefabs にモデルを入れれば、そのまま使えるようになる
				label.text = "PENDING";
				label.color = Shop_Label_Disabled;
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
				label.text = price >= 0 ? "LOCKED\n" + price.ToString() + " CR" : "LOCKED";
				label.color = Shop_Label_Locked;
			}
			else
			{
				bool equipped = ShopManager.SingletonInstance.PlaneModelNumber == i;
				label.text = equipped ? "EQUIPPED" : "AVAILABLE";
				label.color = equipped ? Shop_Label_Equipped : Shop_Label_Available;
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

		// リザルト中も降り続けてほしいので、下の早期リターンより先に進める
		UpdateConfetti();

		// リザルト中は IsPlay が false になる。
		// 下の分岐に落とすと「TAP!」が出てきてリザルトに重なってしまう
		if (isResultShown == true)
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

		float fuelRatio = planeController.CurrentFuel / PlaneController.Max_Fuel;
		fuelSlider.value = fuelRatio;
		UpdateFuelVisual(fuelRatio);
		UpdateTimerVisual();
	}

	/// <summary>
	/// 燃料バーの見た目。残りわずかで赤くし、減った分は残像が遅れて追いかける
	/// </summary>
	/// <param name="fuelRatio">燃料の残り割合（0〜1）</param>
	void UpdateFuelVisual(float fuelRatio)
	{
		if (fuelFillImage != null)
		{
			fuelFillImage.color = fuelRatio <= Fuel_Critical_Ratio ? Hud_Critical : Hud_Orange;
		}

		if (fuelGhostFill == null)
		{
			return;
		}

		if (fuelRatio >= fuelGhostRatio)
		{
			// 補給したときは待たせず即座に追いつく
			fuelGhostRatio = fuelRatio;
		}
		else
		{
			fuelGhostRatio = Mathf.MoveTowards(fuelGhostRatio, fuelRatio, Ghost_Catchup_Per_Second * Time.deltaTime);
		}

		// アンカーで幅を出すので、塗り用のスプライトを用意しなくてよい
		RectTransform rt = fuelGhostFill.rectTransform;
		Vector2 anchorMax = rt.anchorMax;
		anchorMax.x = fuelGhostRatio;
		rt.anchorMax = anchorMax;
	}

	/// <summary>
	/// 残り時間がわずかになったらタイマーを赤くして点滅させる
	/// </summary>
	void UpdateTimerVisual()
	{
		if (gameManager.TotalTime > Timer_Critical_Seconds)
		{
			timerText.color = Hud_Bone;
			return;
		}

		// 1秒周期で明滅させる。Time.time の関数なのでフレームレートに依存しない
		float pulse = Mathf.PingPong(Time.time * 2f, 1f);
		Color c = Hud_Critical;
		c.a = Mathf.Lerp(0.55f, 1f, pulse);
		timerText.color = c;
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
	/// ゴール時に「GOAL」を画面中央に表示します。
	/// フェード用の Image より手前に配置してあるので、暗転した上に重なって出ます。
	/// </summary>
	public void ShowGoalText()
	{
		goalText.gameObject.SetActive(true);

		RectTransform goalRect = goalText.rectTransform;
		goalRect.DOKill();
		// 前のステージで上へ寄せた位置が残っていることがあるので、中央に戻してから出す
		goalRect.anchoredPosition = new Vector2(goalRect.anchoredPosition.x, 0f);
		// 小さい状態から弾むように出す
		goalRect.localScale = Vector3.one * 0.5f;
		// 広告などで timeScale が 0 になっても止まらないように SetUpdate(true) にする
		goalRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
	}

	/// <summary>
	/// ゴール演出とリザルト画面を出す。
	/// 以前はゴールした瞬間にシーンを切り替えていたので、ゴール文字が1フレームしか映らずに
	/// 消えていた。ここではシーンを切り替えず、「NEXT」が押されるまで演出を見せきる
	/// </summary>
	/// <param name="clearTimeSeconds">クリアタイム（秒）</param>
	/// <param name="stageCoin">このステージで拾ったコイン数</param>
	/// <param name="bonusCoin">ゴールボーナスで上乗せしたコイン数</param>
	/// <param name="onNext">「NEXT」が押されたときの処理</param>
	public void ShowResult(float clearTimeSeconds, int stageCoin, int bonusCoin, UnityAction onNext)
	{
		isResultShown = true;

		ShowGoalText();
		PlayConfetti();

		// ゴール文字はリザルトと重なるので、少し遅れて上へ寄せる
		goalText.rectTransform.DOAnchorPosY(Goal_Text_Result_PosY, 0.45f).SetEase(Ease.OutCubic).SetUpdate(true).SetDelay(0.3f);

		if (resultTimeText != null)
		{
			resultTimeText.text = FormatClearTime(clearTimeSeconds);
		}

		if (resultCoinText != null)
		{
			resultCoinText.text = (stageCoin + bonusCoin).ToString();
		}

		if (resultBonusText != null)
		{
			// 内訳を出しておかないと、拾った数と表示が合っていないように見える
			resultBonusText.text = "BASE " + stageCoin + "   BONUS +" + bonusCoin;
		}

		if (nextStageButton != null)
		{
			// UI はステージごとに作り直されるが、押しっぱなしの登録が残らないよう毎回入れ替える
			nextStageButton.onClick.RemoveAllListeners();
			nextStageButton.onClick.AddListener(() =>
			{
				// 連打でシーン切り替えが二重に走らないようにする
				nextStageButton.interactable = false;
				if (onNext != null)
				{
					onNext.Invoke();
				}
			});
			nextStageButton.interactable = true;
		}

		if (panel_Result == null)
		{
			Debug.LogWarning("リザルトパネルが設定されていないため、ゴール文字だけ表示する");
			return;
		}

		panel_Result.SetActive(true);
		// 拡大0から始めるので、遅らせている間は見えない。
		// ゴール文字が上へ抜けるのを待ってから出す
		Transform panelTransform = panel_Result.transform;
		panelTransform.DOKill();
		panelTransform.localScale = Vector3.zero;
		panelTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).SetDelay(0.3f);
	}

	/// <summary>
	/// リザルト画面を閉じる。次のステージへ切り替える直前に呼ぶ
	/// </summary>
	public void HideResult()
	{
		isResultShown = false;
		ClearConfetti();

		goalText.rectTransform.DOKill();
		goalText.gameObject.SetActive(false);

		if (panel_Result != null)
		{
			panel_Result.transform.DOKill();
			panel_Result.SetActive(false);
		}
	}

	/// <summary>
	/// クリアタイムを 分:秒.小数 の形にする
	/// </summary>
	static string FormatClearTime(float seconds)
	{
		// 秒を四捨五入すると 59.999 秒が 60 秒として表示されてしまうので切り捨てる
		int hundredths = Mathf.Max(0, Mathf.FloorToInt(seconds * 100f));
		int minutePart = hundredths / 6000;
		int secondPart = (hundredths / 100) % 60;
		int fractionPart = hundredths % 100;
		return string.Format("{0:00}:{1:00}.{2:00}", minutePart, secondPart, fractionPart);
	}

	/// <summary>
	/// 紙吹雪を降らせる。専用のパーティクルやスプライトを用意せずに済むよう、
	/// 単色の Image を実行時に作って落としている。
	/// スプライトを持たせていないので全部が同じマテリアルになり、枚数を増やしてもまとめて描かれる
	/// </summary>
	void PlayConfetti()
	{
		EnsureConfettiRoot();
		// 前回の分が残っていたら片付けてから出す
		ClearConfetti();

		float halfWidth = confettiRoot.rect.width * 0.5f;
		float halfHeight = confettiRoot.rect.height * 0.5f;
		confettiRemoveY = -halfHeight - 120f;

		for (int i = 0; i < Confetti_Count; i++)
		{
			GameObject piece = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
			RectTransform pieceRect = (RectTransform)piece.transform;
			pieceRect.SetParent(confettiRoot, false);
			pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
			pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
			pieceRect.pivot = new Vector2(0.5f, 0.5f);
			pieceRect.sizeDelta = new Vector2(Random.Range(12f, 22f), Random.Range(20f, 34f));

			Image pieceImage = piece.GetComponent<Image>();
			pieceImage.color = Confetti_Colors[Random.Range(0, Confetti_Colors.Length)];
			// 「NEXT」ボタンを押せなくならないよう、当たり判定は切っておく
			pieceImage.raycastTarget = false;

			ConfettiPiece state = new ConfettiPiece();
			state.Rect = pieceRect;
			state.BaseX = Random.Range(-halfWidth, halfWidth);
			// 画面の上に高く散らして置き、間を空けて降ってくるようにする
			state.PositionY = halfHeight + Random.Range(40f, 1600f);
			state.FallSpeed = Random.Range(420f, 900f);
			state.SwayAmplitude = Random.Range(20f, 110f);
			state.SwayFrequency = Random.Range(2.5f, 6f);
			state.SwayPhase = Random.Range(0f, Mathf.PI * 2f);
			state.SpinSpeed = Random.Range(-320f, 320f);
			state.Angle = Random.Range(0f, 360f);

			ApplyConfettiTransform(state);
			confettiPieces.Add(state);
		}
	}

	/// <summary>
	/// 紙吹雪を1フレーム分進める。
	/// リザルト中も降り続けてほしいので、Update の早期リターンより前から呼ぶ
	/// </summary>
	void UpdateConfetti()
	{
		if (confettiPieces.Count == 0)
		{
			return;
		}

		// 広告表示などで timeScale が 0 になっても止まらないように、時間の影響を受けない値を使う
		float deltaTime = Time.unscaledDeltaTime;

		// 落ちきった分を取り除くので、後ろから回す
		for (int i = confettiPieces.Count - 1; i >= 0; i--)
		{
			ConfettiPiece piece = confettiPieces[i];
			if (piece.Rect == null)
			{
				confettiPieces.RemoveAt(i);
				continue;
			}

			piece.PositionY = piece.PositionY - piece.FallSpeed * deltaTime;
			piece.SwayPhase = piece.SwayPhase + piece.SwayFrequency * deltaTime;
			piece.Angle = piece.Angle + piece.SpinSpeed * deltaTime;

			if (piece.PositionY < confettiRemoveY)
			{
				Destroy(piece.Rect.gameObject);
				confettiPieces.RemoveAt(i);
				continue;
			}

			ApplyConfettiTransform(piece);
		}
	}

	static void ApplyConfettiTransform(ConfettiPiece piece)
	{
		float x = piece.BaseX + Mathf.Sin(piece.SwayPhase) * piece.SwayAmplitude;
		piece.Rect.anchoredPosition = new Vector2(x, piece.PositionY);
		piece.Rect.localEulerAngles = new Vector3(0f, 0f, piece.Angle);
	}

	/// <summary>
	/// 紙吹雪の親を用意する。
	/// ゴール文字と同じ親の一番手前に置くことで、リザルトより前に降らせる
	/// </summary>
	void EnsureConfettiRoot()
	{
		if (confettiRoot != null)
		{
			return;
		}

		GameObject root = new GameObject("Confetti_Root", typeof(RectTransform));
		RectTransform rootRect = (RectTransform)root.transform;
		rootRect.SetParent(goalText.rectTransform.parent, false);
		rootRect.anchorMin = Vector2.zero;
		rootRect.anchorMax = Vector2.one;
		rootRect.offsetMin = Vector2.zero;
		rootRect.offsetMax = Vector2.zero;
		rootRect.SetAsLastSibling();
		confettiRoot = rootRect;
	}

	/// <summary>
	/// 出している紙吹雪をまとめて消す
	/// </summary>
	void ClearConfetti()
	{
		for (int i = 0; i < confettiPieces.Count; i++)
		{
			RectTransform piece = confettiPieces[i].Rect;
			if (piece == null)
			{
				continue;
			}
			Destroy(piece.gameObject);
		}
		confettiPieces.Clear();
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
