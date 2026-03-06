using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームマネージャー
/// </summary>
public class GameManager : MonoBehaviour
{
	private static GameManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static GameManager SingletonInstance => singletonInstance;

	[Tooltip("トータルの制限時間")]
	float totalTime = 30;
	bool isPlay = false;
	public bool IsPlay => isPlay;

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
	}

	void Start()
	{

	}

	void Update()
	{
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

	void TimerSystem()
	{
		totalTime = totalTime - Time.deltaTime;
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
	/// ゲームクリアー
	/// </summary>
	public void GameClear()
	{
		SceneManager.LoadScene("GameClear");
	}

	/// <summary>
	/// ゲームオーバー
	/// </summary>
	public void GameOver()
	{
		SceneManager.LoadScene("GameOver");
	}
}
