using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// UI
/// </summary>
public class UI : MonoBehaviour
{
	[Tooltip("飛行機のモデル")]
	[SerializeField] PlaneController planeController;
	[Tooltip("ゲームマネージャー")]
	[SerializeField] GameManager gameManager;

	[Header("UIに関する変数")]
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

	[Tooltip("タップテキスト")]
	[SerializeField] TextMeshProUGUI tapText;
	private Tween tapTween;

	[Header("Fade")]
	[Tooltip("フェード対象の Image (任意)")]
	[SerializeField] Image fadeImage;
	bool isFade = false;
	public bool IsFade => isFade;

	[Tooltip("コインテキスト")]
	[SerializeField] TextMeshProUGUI coinText;
	public TextMeshProUGUI CoinText => coinText;

	void Start()
	{
		timerText.text = "残り時間：" + gameManager.TotalTime.ToString("f1");
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;

		tapText.transform.localScale = Vector3.one;
		tapTween = tapText.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetAutoKill(false).Pause();
	}

	void Update()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		if (isFade == false)
		{
			return;
		}

		if (gameManager.IsPlay == false)
		{
			tapText.gameObject.SetActive(true);
			tapTween.Play();
			return;
		}
		else
		{
			tapTween.Pause();
			tapText.gameObject.SetActive(false);
			tapText.transform.localScale = Vector3.one;
		}

		timerText.text = "残り時間：" + gameManager.TotalTime.ToString("f1");
		coinText.text = gameManager.CoinCount.ToString();
		fuelSlider.value = planeController.CurrentFuel / PlaneController.Max_Fuel;
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


	/// <summary>
	/// 指定した Image をフェードインします。
	/// 不透明にする
	/// </summary>
	public Tween FadeIn(float duration, TweenCallback onComplete = null)
	{
		if (fadeImage == null)
		{
			return null;
		}

		fadeImage.gameObject.SetActive(true);
		Color color = fadeImage.color;
		color.a = 0f;
		fadeImage.color = color;

		return fadeImage.DOFade(1f, duration).OnComplete(() =>
		{
			onComplete?.Invoke();
		});
	}

	public void FadeIn()
	{
		fadeImage.gameObject.SetActive(true);
		Color color = fadeImage.color;
		color.a = 1f;
		fadeImage.color = color;
	}

	/// <summary>
	/// 指定した Image をフェードアウトします。
	/// 透明にする
	/// </summary>
	public Tween FadeOut(float duration = 1f, bool disableOnComplete = true, TweenCallback onComplete = null)
	{
		if (fadeImage == null)
		{
			return null;
		}

		return fadeImage.DOFade(0f, duration).OnComplete(() =>
		{
			if (disableOnComplete)
			{
				fadeImage.gameObject.SetActive(false);
			}
			isFade = true;
			onComplete?.Invoke();
		});
	}

}
