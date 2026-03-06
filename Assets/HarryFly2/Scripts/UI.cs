using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

	[SerializeField] TextMeshProUGUI tapText;
	private Tween tapTween;

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

		tapText.transform.localScale = Vector3.one;
		tapTween = tapText.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetAutoKill(false).Pause();
	}

	void Update()
	{
		fuelSlider.value = PlaneController.SingletonInstance.CurrentFuel / PlaneController.SingletonInstance.MaxFuel;

		if (GameManager.SingletonInstance.IsPlay == false)
		{
			tapText.gameObject.SetActive(true);
			tapTween.Play();
		}
		else
		{
			tapTween.Pause();
			tapText.gameObject.SetActive(false);
			tapText.transform.localScale = Vector3.one;
		}

		if (GameManager.SingletonInstance.IsPlay == false)
		{
			return;
		}
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
