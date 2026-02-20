using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI
/// </summary>
public class UI : MonoBehaviour
{
	private static UI singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static UI SingletonInstance => singletonInstance;

	[Tooltip("タイマーテキスト")]
	[SerializeField] Text timerText;
	public Text TimerText => timerText;

	[Tooltip("燃料スライダー")]
	[SerializeField] Slider fuelSlider;

	[Tooltip("ジョイスティック")]
	[SerializeField] FloatingJoystick floatingJoystick;
	public FloatingJoystick FloatingJoystick => floatingJoystick;

	[Tooltip("加速ボタン")]
	[SerializeField] Button accelerateButton;
	public Button AccelerateButton => accelerateButton;

	// ボタンを押したときtrue、離したときfalseになるフラグ
	bool buttonDownFlag = false;
	public bool ButtonDownFlag => buttonDownFlag;

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
		fuelSlider.value = PlaneController.SingletonInstance.CurrentFuel / PlaneController.SingletonInstance.MaxFuel;
	}

	void Update()
	{
		fuelSlider.value = PlaneController.SingletonInstance.CurrentFuel / PlaneController.SingletonInstance.MaxFuel;
	}

	// ボタンを押したときの処理
	public void OnButtonDown()
	{
		Debug.Log("Down");
		buttonDownFlag = true;
	}

	// ボタンを離したときの処理
	public void OnButtonUp()
	{
		Debug.Log("Up");
		buttonDownFlag = false;
	}
}
