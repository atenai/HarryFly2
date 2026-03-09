using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ゲームマネージャー
/// </summary>
public class GameManager : MonoBehaviour
{
	[Tooltip("UI")]
	[SerializeField] UI ui;

	/// <summary>ゲームプレイ中かどうか</summary>
	bool isPlay = false;
	public bool IsPlay => isPlay;

	/// <summary>トータルの制限時間</summary>
	float totalTime = 30;

	/// <summary>コイン数</summary>
	int coinCount = 0;
	public int CoinCount => coinCount;
	public static readonly int Max_Coin_Count = 999999;

	void Awake()
	{
		isPlay = false;
		Load();
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
	}

	void Start()
	{
		ui.CoinText.text = coinCount.ToString();
	}

	void Update()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		if (Input.GetMouseButton(0))
		{
			//ここにタップされた時の処理を書く
			isPlay = true;
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
		Debug.Log("残り時間：" + totalTime);
		ui.TimerText.text = "残り時間：" + totalTime.ToString("f1");
		if (totalTime <= 0)
		{
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
		ui.CoinText.text = coinCount.ToString();
	}

	/// <summary>
	/// ゲームクリアー
	/// </summary>
	public void GameClear()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		//セーブ
		ES3.Save("CoinCount", coinCount);
		// シーンを切り替える
		StageManager.SingletonInstance.IsTriggered = true;
	}

	/// <summary>
	/// ゲームオーバー
	/// </summary>
	public void GameOver()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		//セーブ
		ES3.Save("CoinCount", coinCount);
		// シーンを切り替える
		StageManager.SingletonInstance.IsTriggered = true;
	}
}
