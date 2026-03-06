using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ゲームマネージャー
/// </summary>
public class GameManager : MonoBehaviour
{
	private static GameManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static GameManager SingletonInstance => singletonInstance;

	/// <summary>ゲームプレイ中かどうか</summary>
	bool isPlay = false;
	public bool IsPlay => isPlay;

	/// <summary>トータルの制限時間</summary>
	float totalTime = 30;

	/// <summary>コイン数</summary>
	int coinCount = 0;
	public int CoinCount => coinCount;
	public static readonly int Max_Coin_Count = 999999;

	[Header("事前に読み込むシーン名")]
	[SerializeField] string gameClearSceneName = "GameClear";
	[SerializeField] string gameOverSceneName = "GameOver";

	bool isGameClearLoaded = false;
	bool isGameOverLoaded = false;
	bool isSceneSwitched = false;
	public bool IsSceneSwitched => isSceneSwitched;
	bool isGameClearTriggered = false;
	bool isGameOverTriggered = false;

	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
		}
		else
		{
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
		}

		isPlay = false;
		isSceneSwitched = false;
		isGameClearLoaded = false;
		isGameOverLoaded = false;
		isGameClearTriggered = false;
		isGameOverTriggered = false;
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
		UI.SingletonInstance.CoinText.text = GameManager.SingletonInstance.CoinCount.ToString();
		StartCoroutine(PreloadScenesCoroutine());
	}

	/// <summary>
	/// ゲームクリアーシーンとゲームオーバーシーンを事前ロードする
	/// </summary>
	IEnumerator PreloadScenesCoroutine()
	{
		yield return StartCoroutine(LoadSceneAdditiveAndHide(gameClearSceneName));
		yield return StartCoroutine(LoadSceneAdditiveAndHide(gameOverSceneName));
	}

	/// <summary>
	/// シーンを Additive で非同期ロードし、読み込み後にルートオブジェクトを非表示にする
	/// </summary>
	IEnumerator LoadSceneAdditiveAndHide(string sceneName)
	{
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

		while (asyncOperation.isDone == false)
		{
			yield return null;
		}

		Scene loadedScene = SceneManager.GetSceneByName(sceneName);

		if (loadedScene.IsValid() == true && loadedScene.isLoaded == true)
		{
			GameObject[] rootObjects = loadedScene.GetRootGameObjects();

			for (int i = 0; i < rootObjects.Length; i++)
			{
				rootObjects[i].SetActive(false);
			}
		}

		if (sceneName == gameClearSceneName)
		{
			isGameClearLoaded = true;
		}
		else if (sceneName == gameOverSceneName)
		{
			isGameOverLoaded = true;
		}
	}

	void Update()
	{
		if (isSceneSwitched == true)
		{
			return;
		}

		ShowGameClearScene();
		ShowGameOverScene();

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
	/// ゲームクリアー画面へ切り替える
	/// </summary>
	void ShowGameClearScene()
	{
		if (isGameClearTriggered == false)
		{
			return;
		}

		if (isSceneSwitched == true)
		{
			return;
		}

		if (isGameClearLoaded == false)
		{
			Debug.Log("GameClearシーンがまだ読み込まれていません。");
			return;
		}

		isSceneSwitched = true;
		ShowScene(gameClearSceneName);
	}

	/// <summary>
	/// ゲームオーバー画面へ切り替える
	/// </summary>
	void ShowGameOverScene()
	{
		if (isGameOverTriggered == false)
		{
			return;
		}

		if (isSceneSwitched == true)
		{
			return;
		}

		if (isGameOverLoaded == false)
		{
			Debug.Log("GameOverシーンがまだ読み込まれていません。");
			return;
		}

		isSceneSwitched = true;
		ShowScene(gameOverSceneName);
	}

	/// <summary>
	/// 指定シーンを表示し、そのシーンをアクティブにする
	/// </summary>
	void ShowScene(string sceneName)
	{
		Scene targetScene = SceneManager.GetSceneByName(sceneName);

		if (targetScene.IsValid() == false || targetScene.isLoaded == false)
		{
			Debug.LogWarning(sceneName + " シーンが見つかりません。");
			return;
		}

		GameObject[] rootObjects = targetScene.GetRootGameObjects();

		for (int i = 0; i < rootObjects.Length; i++)
		{
			rootObjects[i].SetActive(true);
		}

		SceneManager.SetActiveScene(targetScene);
	}

	/// <summary>
	/// 制限時間のシステム
	/// </summary>
	void TimerSystem()
	{
		totalTime = totalTime - Time.deltaTime;
		Debug.Log("残り時間：" + totalTime);
		UI.SingletonInstance.TimerText.text = "残り時間：" + totalTime.ToString("f1");
		if (totalTime <= 0)
		{
			GameOver();
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
		UI.SingletonInstance.CoinText.text = coinCount.ToString();
	}

	/// <summary>
	/// ゲームクリアー
	/// </summary>
	public void GameClear()
	{
		//セーブ
		ES3.Save("CoinCount", coinCount);
		// シーンを切り替える
		isGameClearTriggered = true;
	}

	/// <summary>
	/// ゲームオーバー
	/// </summary>
	public void GameOver()
	{
		// シーンを切り替える
		ShowGameOverScene();
		isGameOverTriggered = true;
	}
}
