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

	bool isLoaded = false;
	public bool IsLoaded
	{
		get { return isLoaded; }
		set { isLoaded = value; }
	}
	bool isSceneSwitched = false;
	public bool IsSceneSwitched
	{
		get { return isSceneSwitched; }
		set { isSceneSwitched = value; }
	}
	bool isTriggered = false;
	public bool IsTriggered
	{
		get { return isTriggered; }
		set { isTriggered = value; }
	}

	//現在のシーンのビルドインデックス
	int currentSceneIndex = 0;

	//次のシーンのビルドインデックス
	int nextSceneIndex = 0;

	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
			DontDestroyOnLoad(this.gameObject);//シーンを切り替えた時に破棄しない
		}
		else
		{
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
		}

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
		UI.SingletonInstance.CoinText.text = GameManager.SingletonInstance.CoinCount.ToString();
		InitScene();
		LoadNextStage();
	}

	void InitScene()
	{
		GameManager.SingletonInstance.IsSceneSwitched = false;
		GameManager.SingletonInstance.IsLoaded = false;
		GameManager.SingletonInstance.IsTriggered = false;
	}

	/// <summary>
	/// 次のステージをロードする
	/// </summary>
	void LoadNextStage()
	{
		//最初のステージのシーン番号
		const int firstStageBuildIndex = 0;
		//最後のステージのシーン番号を取得する
		int lastStageBuildIndex = SceneManager.sceneCountInBuildSettings - 1;
		//現在のシーン番号を取得する
		currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
		//次のシーン番号を計算する
		nextSceneIndex = currentSceneIndex + 1;

		// 最後のステージを超えたら最初のステージに戻る
		if (lastStageBuildIndex < nextSceneIndex)
		{
			nextSceneIndex = firstStageBuildIndex;
		}

		StartCoroutine(PreloadScenesCoroutine("Stage" + nextSceneIndex));
	}

	/// <summary>
	/// シーンを事前ロードする
	/// </summary>
	IEnumerator PreloadScenesCoroutine(string sceneName)
	{
		yield return StartCoroutine(LoadSceneAdditiveAndHide(sceneName));
	}

	/// <summary>
	/// シーンをAdditiveで非同期ロードし、読み込み後にルートオブジェクトを非表示にする
	/// </summary>
	IEnumerator LoadSceneAdditiveAndHide(string sceneName)
	{
		if (Application.CanStreamedLevelBeLoaded(sceneName) == false)
		{
			Debug.LogError(sceneName + " が Build Settings に含まれていないか、名前が一致しません。");
			yield break;
		}

		Debug.Log("Start loading scene: " + sceneName);

		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

		if (asyncOperation == null)
		{
			Debug.LogError("LoadSceneAsync failed for: " + sceneName);
			yield break;
		}

		// シンプルに AsyncOperation を待機し、その後シーンが実際にロードされたか確認する
		yield return asyncOperation;

		Scene loadedScene = SceneManager.GetSceneByName(sceneName);

		// AsyncOperation が完了してもシーン取得に時間が掛かる場合があるため、短いタイムアウト付きで確認する
		int checks = 0;
		while ((loadedScene.IsValid() == false || loadedScene.isLoaded == false) && checks < 60)
		{
			Debug.Log("Waiting for scene to become available: " + sceneName + " (check=" + checks + ")");
			checks++;
			yield return null;
			loadedScene = SceneManager.GetSceneByName(sceneName);
		}

		if (loadedScene.IsValid() == true && loadedScene.isLoaded == true)
		{
			GameObject[] rootObjects = loadedScene.GetRootGameObjects();

			for (int i = 0; i < rootObjects.Length; i++)
			{
				rootObjects[i].SetActive(false);
			}
		}
		else
		{
			Debug.LogWarning(sceneName + " のロードが完了していません。");
		}

		GameManager.SingletonInstance.IsLoaded = true;

		Debug.Log("Finished loading scene: " + sceneName);
	}

	void Update()
	{
		if (isSceneSwitched == true)
		{
			return;
		}

		ShowNextStage();

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

	void ShowNextStage()
	{
		if (isLoaded == false)
		{
			Debug.Log("シーンがまだ読み込まれていません。");
			return;
		}

		if (isTriggered == false)
		{
			Debug.Log("シーン切り替えのトリガーがまだ発生していません。");
			return;
		}

		if (isSceneSwitched == true)
		{
			Debug.Log("シーンはすでに切り替えられています。");
			return;
		}

		isSceneSwitched = true;
		OnUnloadScene("stage" + currentSceneIndex);
		ShowScene("Stage" + nextSceneIndex);
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

	void OnUnloadScene(string sceneName)
	{
		StartCoroutine(CoUnload(sceneName));
	}

	IEnumerator CoUnload(string sceneName)
	{
		//Sceneをアンロード
		var op = SceneManager.UnloadSceneAsync(sceneName);
		yield return op;

		//アンロード後の処理を書く
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
		if (isSceneSwitched == true)
		{
			return;
		}

		//セーブ
		ES3.Save("CoinCount", coinCount);
		// シーンを切り替える
		isTriggered = true;
	}

	/// <summary>
	/// ゲームオーバー
	/// </summary>
	public void GameOver()
	{
		if (isSceneSwitched == true)
		{
			return;
		}

		//セーブ
		ES3.Save("CoinCount", coinCount);
		// シーンを切り替える
		isTriggered = true;
	}
}
