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

	/// <summary>ゲームプレイ中かどうか</summary>
	bool isPlay = false;
	public bool IsPlay
	{
		get { return isPlay; }
		set { isPlay = value; }
	}

	/// <summary>トータルの制限時間</summary>
	float totalTime = 30;
	public float TotalTime => totalTime;

	/// <summary>コイン数</summary>
	int coinCount = 0;
	public int CoinCount => coinCount;
	public static readonly int Max_Coin_Count = 999999;

	/// <summary>最後にセーブしたコイン数。同じ値を何度も書き込まないための比較用</summary>
	int savedCoinCount = 0;

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
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		if (hasStartedFadeOut == false)
		{
			hasStartedFadeOut = true;
			ui.FadeOut();
		}

		if (ui.IsFade == false)
		{
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
		totalTime = totalTime - Time.deltaTime;
		//Debug.Log("残り時間：" + totalTime);
		if (totalTime <= 0)
		{
			// 時間切れもステージの切り替えなので、切り替わる前にコインを保存する
			SaveCoin();
			// シーンを切り替える
			StageManager.SingletonInstance.IsTriggered = true;
		}
	}

	/// <summary>
	/// 時間の追加
	/// </summary>
	/// <param name="value">追加量</param>
	public void AddTimer(float value)
	{
		totalTime = totalTime + value;
	}

	/// <summary>
	/// コインの追加
	/// </summary>
	/// <param name="value"></param>
	public void AddCoin(int value)
	{
		coinCount = coinCount + value;
		if (Max_Coin_Count <= coinCount)
		{
			coinCount = Max_Coin_Count;
		}
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
