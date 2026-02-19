using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲームマネージャー
/// </summary>
public class GameManager : MonoBehaviour
{
	private static GameManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static GameManager SingletonInstance => singletonInstance;

	//UI
	public GameObject gameClearUI;
	public GameObject gameOverUI;
	//timer
	public Text timer;
	[Tooltip("トータルの制限時間")]
	[SerializeField] float totalTime = 30;

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
	}

	void Start()
	{
		Screen.SetResolution(1920, 1080, true, 60);

		gameClearUI.SetActive(false);
		gameOverUI.SetActive(false);

		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}

		totalTime -= Time.deltaTime;
		timer.text = "残り時間：" + totalTime.ToString("f1");
		if (totalTime <= 0)
		{
			GameOver();
		}
	}

	public void Replay()
	{
		Time.timeScale = 1;
		SceneManager.LoadScene("PlayScene");
	}

	//To menu
	public void ToMenu()
	{
		Time.timeScale = 1;
		SceneManager.LoadScene("TitleScene");
	}

	/// <summary>
	/// 時間を追加
	/// </summary>
	/// <param name="value">追加量</param>
	public void AddTimer(float value)
	{
		totalTime = totalTime + value;
	}

	/// <summary>
	/// ゲームクリアー
	/// </summary>
	public void GameClear()
	{
		gameClearUI.SetActive(true);

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	/// <summary>
	/// ゲームオーバー
	/// </summary>
	public void GameOver()
	{
		gameOverUI.SetActive(true);

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}
}
