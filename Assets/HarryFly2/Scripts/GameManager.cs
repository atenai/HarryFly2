using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// ゲームマネージャー
/// </summary>
public class GameManager : MonoBehaviour
{
	[Tooltip("UI")]
	[SerializeField] UI ui;

	/// <summary>フェードアウトを開始済みかどうか</summary>
	bool hasStartedFadeOut = false;

	/// <summary>フェード完了を待っている時間（秒）</summary>
	float fadeWaitTime = 0f;

	/// <summary>この秒数を過ぎてもフェードが終わらなければ強制的に解除する</summary>
	const float Fade_Timeout_Seconds = 3f;

	/// <summary>
	/// タイムアウト計測で1フレームに加算できる上限（秒）。
	/// 起動直後やシーンロード直後の1フレームは数秒に達することがあり、
	/// 上限を設けないとフェードが始まる前にタイムアウトが誤発火してしまう
	/// </summary>
	const float Fade_Wait_Step_Max = 0.1f;

	/// <summary>
	/// ステージ開始時に暗転が明けるまでの時間（秒）。
	/// 次ステージは事前ロード済みなので待たせる必要がなく、短いほどテンポが良くなる
	/// </summary>
	const float Fade_Out_Seconds = 0.35f;

	/// <summary>ゲームプレイ中かどうか</summary>
	bool isPlay = false;
	public bool IsPlay
	{
		get { return isPlay; }
		set { isPlay = value; }
	}

	/// <summary>
	/// トータルの制限時間。ステージごとに変えられるよう、
	/// 各シーンの GameManager インスタンス側で上書きする
	/// </summary>
	[Tooltip("このステージの制限時間（秒）。ステージの距離に合わせて設定する")]
	[SerializeField] float totalTime = 30;
	public float TotalTime => totalTime;

	/// <summary>コイン数</summary>
	int coinCount = 0;
	public int CoinCount => coinCount;
	public static readonly int Max_Coin_Count = 999999;

	/// <summary>最後にセーブしたコイン数。同じ値を何度も書き込まないための比較用</summary>
	int savedCoinCount = 0;

	/// <summary>
	/// このステージで拾ったコイン数。ゴール時の2倍ボーナスの計算に使う。
	/// GameManager はステージごとに作り直されるので、自動的に0から始まる
	/// </summary>
	int stageCoinCount = 0;
	public int StageCoinCount => stageCoinCount;

	/// <summary>
	/// ゲーム開始から実際にプレイしていた時間（秒）。リザルトのクリアタイムに使う。
	/// 残り時間（totalTime）はアイテムで増えるので、経過時間の代わりには使えない
	/// </summary>
	float playTime = 0f;
	public float PlayTime => playTime;

	void Awake()
	{
		isPlay = false;
	}

	void Start()
	{
		Load();
		ui.FadeIn();
		CoinText();
	}

	void Load()
	{
		//セーブデータの読み込み
		if (ES3.KeyExists("CoinCount"))
		{
			coinCount = ES3.Load<int>("CoinCount");
		}
		else
		{
			coinCount = 0;
		}
		savedCoinCount = coinCount;
	}

	void CoinText()
	{
		ui.CoinText.text = coinCount.ToString();
	}

	void Update()
	{
		// シングルトンがまだ入っていないフレームで例外を出すと、
		// 下のフェード開始処理まで到達できず操作不能のまま固まる
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager != null && stageManager.IsSceneSwitched == true)
		{
			return;
		}

		if (hasStartedFadeOut == false)
		{
			// トゥイーンが返ってきたときだけ「開始済み」にする。
			// 先にフラグを立てると、一度失敗しただけで二度と再試行できなくなる
			if (ui.FadeOut(Fade_Out_Seconds) != null)
			{
				hasStartedFadeOut = true;
			}
		}

		if (ui.IsFade == false)
		{
			// フェードが完了しないと全操作が止まったままになるので、一定時間で強制的に解除する。
			// フェードを開始できた後だけ数え、1フレームの加算にも上限を設ける
			if (hasStartedFadeOut == true)
			{
				fadeWaitTime = fadeWaitTime + Mathf.Min(Time.unscaledDeltaTime, Fade_Wait_Step_Max);
				if (Fade_Timeout_Seconds <= fadeWaitTime)
				{
					Debug.LogWarning("フェードが完了しなかったため強制的に解除する");
					ui.ForceFadeComplete();
				}
			}
			return;
		}

		if (isPlay == false)
		{
			return;
		}

		TimerSystem();
	}

	/// <summary>
	/// 制限時間のシステム
	/// </summary>
	void TimerSystem()
	{
		playTime = playTime + Time.deltaTime;
		totalTime = totalTime - Time.deltaTime;

		if (0 < totalTime)
		{
			return;
		}

		// totalTime に下限を設けていないので、切り替わるまでここは毎フレーム通り続ける。
		// 音も演出もシーン切り替えも一度きりにする
		if (hasPlayedTimeUp == true)
		{
			return;
		}
		hasPlayedTimeUp = true;

		// 保存より先に鳴らす。ES3 の書き込みは同期なのでフレームが止まり、
		// その後だと音の立ち上がりが遅れて聞こえる
		PlayTimeUpSound();

		// 電波が切れた見立ての演出を出す
		if (ui != null)
		{
			ui.ShowSignalLost();
		}

		// 操縦できなくなったので、このステージの動くものを止める。
		// 対空機関砲は IsPlay を見ているので、ここを下ろせば撃つのをやめる
		isPlay = false;
		StopStageForSignalLost();

		// 時間切れもステージの切り替えなので、切り替わる前にコインを保存する
		SaveCoin();

		// ここで直接切り替えると、演出が1フレームも映らないまま次のステージに行く。
		// 衝突時に爆発を見せてから切り替えているのと同じ考え方
		StartCoroutine(SwitchStageAfterSignalLost());
	}

	[Tooltip("通信不能の演出を見せてから次のステージへ切り替えるまでの時間（秒）")]
	[SerializeField] float signalLostViewSeconds = 1.4f;

	/// <summary>
	/// 通信不能を見せてからステージを切り替える。
	///
	/// 機体は止めて画面からも消してあるので、この間に時計アイテムを拾って
	/// 復帰することはない。演出を見せるためだけの待ち時間
	/// </summary>
	IEnumerator SwitchStageAfterSignalLost()
	{
		yield return new WaitForSeconds(signalLostViewSeconds);
		StageManager.SingletonInstance.IsTriggered = true;
	}

	/// <summary>
	/// このステージで動いているものを止める。
	///
	/// 次のステージは飛行中に裏で読み込んであり、そちらにも機体や障害物が居る。
	/// シーンを見ないと、まだ始まっていない次のステージまで止めてしまう
	/// </summary>
	void StopStageForSignalLost()
	{
		Scene scene = this.gameObject.scene;

		foreach (var plane in FindObjectsOfType<PlaneController>())
		{
			if (plane.gameObject.scene != scene) { continue; }
			plane.StopForSignalLost();
		}

		foreach (var mover in FindObjectsOfType<ObstacleMover>())
		{
			if (mover.gameObject.scene != scene) { continue; }
			mover.StopMoving();
		}

		// 撃たれたあとの弾は砲とは別に飛び続けるので、こちらも止める
		foreach (var bullet in FindObjectsOfType<AntiAirBullet>())
		{
			if (bullet.gameObject.scene != scene) { continue; }
			bullet.StopMoving();
		}
	}

	[Header("効果音")]
	/// <summary>
	/// 時間切れの音。
	///
	/// ステージの終わり方は「ゴール」「墜落」「時間切れ」の3つあるが、
	/// 前2つには音があるのに時間切れだけ無音で、しかも暗転も入らずに
	/// 次のステージへ飛んでいた。失敗したことにすら気づけない
	/// </summary>
	[Tooltip("時間切れの音")]
	[SerializeField] AudioClip timeUpSound;

	[Tooltip("時間切れ音の音量")]
	[SerializeField, Range(0f, 1f)] float timeUpVolume = 0.7f;

	/// <summary>時間切れの音を鳴らしたかどうか</summary>
	bool hasPlayedTimeUp = false;

	/// <summary>
	/// 時間切れの音を鳴らす。
	///
	/// この GameManager はステージごとのシーンに置かれていて、
	/// 直後のアンロードで AudioSource ごと消える。
	/// 爆発音（PlaneController.PlayExplosionSound）と同じく、
	/// シーンをまたいで生き残る入れ物を作ってそこで鳴らす
	/// </summary>
	void PlayTimeUpSound()
	{
		if (timeUpSound == null)
		{
			return;
		}

		GameObject soundObject = new GameObject("TimeUpSound");
		DontDestroyOnLoad(soundObject);

		AudioSource source = soundObject.AddComponent<AudioSource>();
		source.clip = timeUpSound;
		source.volume = timeUpVolume;
		source.spatialBlend = 0f;
		source.Play();

		Destroy(soundObject, timeUpSound.length + 0.1f);
	}

	/// <summary>
	/// 時間の追加
	/// </summary>
	/// <param name="value">追加量</param>
	public void AddTimer(float value)
	{
		totalTime = totalTime + value;

		// 時間が戻ったら、次に切れたときにまた鳴らせるようにする
		if (0 < totalTime)
		{
			hasPlayedTimeUp = false;
		}
	}

	/// <summary>
	/// コインの追加
	/// </summary>
	/// <param name="value"></param>
	public void AddCoin(int value)
	{
		coinCount = coinCount + value;
		stageCoinCount = stageCoinCount + value;
		if (Max_Coin_Count <= coinCount)
		{
			coinCount = Max_Coin_Count;
		}
	}

	/// <summary>
	/// ゴール報酬。このステージで拾った分と同額を上乗せして2倍にする。
	/// </summary>
	/// <returns>上乗せしたコイン数</returns>
	public int ApplyGoalBonus()
	{
		int bonus = stageCoinCount;
		if (bonus <= 0)
		{
			return 0;
		}

		coinCount = coinCount + bonus;
		if (Max_Coin_Count <= coinCount)
		{
			coinCount = Max_Coin_Count;
		}
		return bonus;
	}

	/// <summary>
	/// リワード広告の報酬コインを加算して即座に保存する。
	/// ステージ取得分（stageCoinCount）には加えない。
	/// 加えてしまうと、ショップで広告を見てからゴールするだけで報酬まで2倍になってしまう
	/// </summary>
	/// <param name="value">加算するコイン数</param>
	public void AddRewardCoin(int value)
	{
		if (value <= 0)
		{
			return;
		}

		coinCount = coinCount + value;
		if (Max_Coin_Count <= coinCount)
		{
			coinCount = Max_Coin_Count;
		}
		SaveCoin();
	}

	/// <summary>
	/// コイン数をセーブする。
	/// コインを拾うたびに書き込むと取得が密集したところで処理落ちするので、
	/// ステージが切り替わる直前（ゴール・障害物への衝突・時間切れ）にまとめて保存する。
	/// 前回セーブから値が変わっていなければ書き込まないので、毎フレーム呼んでも問題ない
	/// </summary>
	public void SaveCoin()
	{
		if (coinCount == savedCoinCount)
		{
			return;
		}
		savedCoinCount = coinCount;
		ES3.Save("CoinCount", coinCount);
	}

	/// <summary>
	/// アプリがバックグラウンドへ回るときにコインを保存する。
	/// ステージの途中で中断されると、その周回で拾った分が消えてしまうため。
	/// Androidでは OnApplicationQuit が呼ばれる保証がないので、こちらで受ける
	/// </summary>
	/// <param name="isPaused">バックグラウンドへ回ったかどうか</param>
	void OnApplicationPause(bool isPaused)
	{
		if (isPaused == false)
		{
			return;
		}
		SaveCoin();
	}

	/// <summary>
	/// コインを消費する。指定額以上あれば消費してtrueを返す。
	/// </summary>
	public bool SpendCoin(int amount)
	{
		if (amount <= 0) return false;
		if (coinCount < amount) return false;
		coinCount -= amount;
		if (coinCount < 0) coinCount = 0;
		// 購入だけはステージ切り替えを待たずに即セーブする。
		// ShopManager 側はアンロック状態をその場で保存するので、
		// ここを遅らせるとアプリを落としたときに機体だけ残ってコインが戻ってしまう
		SaveCoin();
		if (ui != null)
		{
			ui.CoinText.text = coinCount.ToString();
		}
		return true;
	}

	void OnGUI()
	{
#if UNITY_EDITOR//Unityエディター上での処理

		GUIStyle styleGreen = new GUIStyle();
		styleGreen.fontSize = 30;
		GUIStyleState styleStateGreen = new GUIStyleState();
		styleStateGreen.textColor = Color.green;
		styleGreen.normal = styleStateGreen;

		GUIStyle styleRed = new GUIStyle();
		styleRed.fontSize = 30;
		GUIStyleState styleStateRed = new GUIStyleState();
		styleStateRed.textColor = Color.red;
		styleRed.normal = styleStateRed;

		GUIStyle styleBlack = new GUIStyle();
		styleBlack.fontSize = 30;
		GUIStyleState styleStateBlack = new GUIStyleState();
		styleStateBlack.textColor = Color.black;
		styleBlack.normal = styleStateBlack;

		GUIStyle styleYellow = new GUIStyle();
		styleYellow.fontSize = 30;
		GUIStyleState styleStateYellow = new GUIStyleState();
		styleStateYellow.textColor = Color.yellow;
		styleYellow.normal = styleStateYellow;

		int lineHeight = 50;

		GUI.Box(new Rect(10, 2 * lineHeight, 100, 50), "コイン", styleYellow);
		GUI.Box(new Rect(350, 2 * lineHeight, 100, 50), coinCount.ToString(), styleRed);
#endif //終了  
	}
}
