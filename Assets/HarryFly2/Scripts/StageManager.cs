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

	/// <summary>
	/// いま事前ロード中のシーン名。
	/// sceneLoaded で「先読みしたシーンだけ」を消すための照合に使う
	/// </summary>
	string preloadingSceneName = null;

	void Awake()
	{
		// 非同期ロードの優先度を上げる。既定の BelowNormal のままだと、
		// ブーストで飛ばしたときに次ステージのロードが間に合わずゴールで待たされる。
		// 読み込み中のフレーム落ちが気になる場合は Normal に下げる。
		Application.backgroundLoadingPriority = ThreadPriority.High;

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

		// 読み込みが終わった瞬間に消せるよう、ロードを始める前に受け取り口を用意しておく
		preloadingSceneName = sceneName;
		SceneManager.sceneLoaded += OnPreloadedSceneLoaded;

		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

		if (asyncOperation == null)
		{
			Debug.LogError("LoadSceneAsync failed for: " + sceneName);
			SceneManager.sceneLoaded -= OnPreloadedSceneLoaded;
			preloadingSceneName = null;
			yield break;
		}

		// シンプルに AsyncOperation を待機し、その後シーンが実際にロードされたか確認する
		yield return asyncOperation;

		SceneManager.sceneLoaded -= OnPreloadedSceneLoaded;

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
			// 通常は sceneLoaded で消え済み。取りこぼしたときのための保険
			HideSceneRoots(loadedScene);
		}
		else
		{
			Debug.LogWarning(sceneName + " のロードが完了していません。");
		}

		preloadingSceneName = null;
		IsLoaded = true;

		Debug.Log("Finished loading scene: " + sceneName);
	}

	/// <summary>
	/// 先読みしたシーンを、読み込まれた「その場」で消す。
	///
	/// コルーチンの再開（yield return asyncOperation の続き）は Update フェーズなので、
	/// そこで消していると、シーンが組み込まれた EarlyUpdate から Update までの間にある
	/// FixedUpdate（＝物理演算）を1回以上通過してしまう。
	/// 全ステージは同じワールド座標を使っている（機体は必ず原点、壁は z=600 など）ため、
	/// その1ステップの間だけ「次ステージの壁・地面・ゴール」が現ステージの飛行中の機体と
	/// 同じ場所に実体を持ち、何も無い場所で爆発したり勝手にゴールしたりしていた。
	/// sceneLoaded はシーン組み込みと同じ EarlyUpdate 内で呼ばれるので、
	/// ここで消せば物理演算に一度も触れさせずに済む
	/// </summary>
	void OnPreloadedSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Additive)
		{
			return;
		}

		if (scene.name != preloadingSceneName)
		{
			return;
		}

		HideSceneRoots(scene);
	}

	/// <summary>
	/// シーン内のルートオブジェクトをまとめて非表示にする
	/// </summary>
	/// <param name="scene">対象のシーン</param>
	void HideSceneRoots(Scene scene)
	{
		GameObject[] rootObjects = scene.GetRootGameObjects();

		for (int i = 0; i < rootObjects.Length; i++)
		{
			rootObjects[i].SetActive(false);
		}
	}

	void OnDestroy()
	{
		// ロードの途中で破棄されても購読が残らないようにする
		SceneManager.sceneLoaded -= OnPreloadedSceneLoaded;
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
		// ここは待機中も毎フレーム通る。以前はそれぞれ Debug.Log を出していたが、
		// 1分で5000件を超えるログが出て実機のフレームレートを落としていたので出さない。
		// （Debug.Log は文字列生成とlogcatへの書き出しが毎回発生する）
		if (IsLoaded == false)
		{
			return;
		}

		if (IsTriggered == false)
		{
			return;
		}

		if (IsSceneSwitched == true)
		{
			return;
		}

		IsSceneSwitched = true;
		PlayTransitionSound();
		OnUnloadScene("Stage" + currentSceneIndex);
		ShowScene("Stage" + nextSceneIndex);
	}

	[Tooltip("ステージが切り替わるときの音")]
	[SerializeField] AudioClip transitionSound;

	[Tooltip("ステージ切り替え音の音量")]
	[SerializeField, Range(0f, 1f)] float transitionVolume = 0.5f;

	/// <summary>ステージ切り替え音の再生元。切り替えのたびに作り直さない</summary>
	AudioSource transitionAudioSource;

	/// <summary>
	/// ステージが切り替わる瞬間の音を鳴らす。
	///
	/// 鳴らす側はこの StageManager でなければならない。
	/// 直後の UnloadSceneAsync で旧ステージの AudioSource は全部消えるが、
	/// StageManager だけは DontDestroyOnLoad なので鳴らし切れる。
	///
	/// 呼び出し元は IsSceneSwitched のガードを通った後なので1回しか来ない
	/// </summary>
	void PlayTransitionSound()
	{
		if (transitionSound == null)
		{
			return;
		}

		if (transitionAudioSource == null)
		{
			transitionAudioSource = this.gameObject.AddComponent<AudioSource>();
			transitionAudioSource.playOnAwake = false;
			transitionAudioSource.spatialBlend = 0f;
		}

		transitionAudioSource.PlayOneShot(transitionSound, transitionVolume);
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

	/// <summary>
	/// 最後のステージのシーン番号
	/// </summary>
	public int LastStageBuildIndex => SceneManager.sceneCountInBuildSettings - 1;

	/// <summary>
	/// いま表示しているステージのシーン番号
	/// </summary>
	public int CurrentStageBuildIndex => SceneManager.GetActiveScene().buildIndex;

	/// <summary>
	/// 指定したステージへ直接飛ぶ。デバッグ用。
	///
	/// 通常の進行（ShowNextStage）は「事前ロード済みの次ステージ」しか出せないので、
	/// 任意のステージへ行くにはシーンを読み直すしかない。
	/// Single で読み込むと事前ロード中のシーンごと片付くため、
	/// 全ステージが同じワールド座標を使っていても二重に存在しない
	/// </summary>
	/// <param name="buildIndex">行き先のシーン番号</param>
	public void JumpToStage(int buildIndex)
	{
		if (buildIndex < 0 || LastStageBuildIndex < buildIndex)
		{
			Debug.LogWarning("ステージ番号が範囲外です: " + buildIndex + "（0〜" + LastStageBuildIndex + "）");
			return;
		}

		// 走っている事前ロードとアンロードを止める。
		// 残しておくと、飛んだ先で「前のステージの続き」を読み込みにいってしまう
		StopAllCoroutines();
		StartCoroutine(JumpToStageCoroutine(buildIndex));
	}

	IEnumerator JumpToStageCoroutine(int buildIndex)
	{
		// 事前ロードの受け取り口を先に外す。
		// 読み込み中のシーンがあると、飛んだ先で消される側になってしまう
		SceneManager.sceneLoaded -= OnPreloadedSceneLoaded;
		preloadingSceneName = null;

		// 切り替え中は機体を止めておく。飛んだ先に着くまで操作させない
		IsSceneSwitched = true;

		string sceneName = "Stage" + buildIndex;
		Debug.Log(sceneName + " へ移動します。");

		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		if (asyncOperation == null)
		{
			Debug.LogError("LoadSceneAsync failed for: " + sceneName);
			IsSceneSwitched = false;
			yield break;
		}

		yield return asyncOperation;

		// 飛んだ先を起点にやり直す。次ステージの事前ロードもここから始まる
		InitScene();
		LoadNextStage();
	}
}
