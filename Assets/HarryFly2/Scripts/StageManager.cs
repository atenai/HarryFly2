using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージマネージャー
/// </summary>
public class StageManager : MonoBehaviour
{
	private static StageManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static StageManager SingletonInstance => singletonInstance;

	//現在のシーンのビルドインデックス
	int currentSceneIndex = 0;

	//次のシーンのビルドインデックス
	int nextSceneIndex = 0;

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
	}

	void Start()
	{
		Debug.Log("現在のステージのシーン番号を取得する" + SceneManager.GetActiveScene().buildIndex);
		InitScene();
		LoadNextStage();
	}

	void InitScene()
	{
		IsSceneSwitched = false;
		IsLoaded = false;
		IsTriggered = false;
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

		IsLoaded = true;

		Debug.Log("Finished loading scene: " + sceneName);
	}

	void Update()
	{
		if (IsSceneSwitched == true)
		{
			return;
		}

		ShowNextStage();
	}

	void ShowNextStage()
	{
		if (IsLoaded == false)
		{
			Debug.Log("シーンがまだ読み込まれていません。");
			return;
		}

		if (IsTriggered == false)
		{
			Debug.Log("シーン切り替えのトリガーがまだ発生していません。");
			return;
		}

		if (IsSceneSwitched == true)
		{
			Debug.Log("シーンはすでに切り替えられています。");
			return;
		}

		IsSceneSwitched = true;
		OnUnloadScene("Stage" + currentSceneIndex);
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
		Debug.Log(sceneName + " をアンロードしました。");
		InitScene();
		LoadNextStage();
	}
}
