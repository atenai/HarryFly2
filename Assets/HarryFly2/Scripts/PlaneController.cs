using UnityEngine;

/// <summary>
/// 飛行機コントローラー
/// </summary>
public class PlaneController : MonoBehaviour
{
	[Tooltip("ゲームマネージャー")]
	[SerializeField] GameManager gameManager;
	[Tooltip("UI")]
	[SerializeField] UI ui;

	[Header("飛行機に関する変数")]
	[Tooltip("飛行機のモデル")]
	[SerializeField] GameObject planePrefab;
	[Tooltip("リジッドボディ")]
	[SerializeField] Rigidbody rb;

	[Tooltip("追加の自動前進速度")]
	float addForwordMoveSpeed = 300f;
	[Tooltip("追加の上下左右移動速度")]
	float addVerticalAndHorizontalMoveSpeed = 200f;
	[Tooltip("自動前進速度の初期値")]
	float initForwordMoveSpeed;
	[Tooltip("上下左右移動速度の初期値")]
	float initVerticalAndHorizontalMoveSpeed;
	[Tooltip("通常時と加速時の自動前進速度を徐々に変える値")]
	float changeForwordMovepeed = 2f;
	[Tooltip("通常時と加速時の上下左右移動速度を徐々に変える値")]
	float changeVerticalAndHorizontalMoveSpeed = 1f;

	[Tooltip("上下の移動制限範囲")]
	float verticalMin = -50f;
	float verticalMax = 50f;
	[Tooltip("左右の移動制限範囲")]
	float horizontalMin = -50f;
	float horizontalMax = 50f;

	[Tooltip("上下の機体回転速度")]
	float verticalRotateSpeed = 20;
	[Tooltip("左右の機体回転速度")]
	float horizontalRotateSpeed = 40;
	[Tooltip("y軸の機体回転速度")]
	float yRotateSpeed = 40;

	/// <summary>現在の燃料 </summary>
	float currentFuel = 100f;
	public float CurrentFuel => currentFuel;

	/// <summary> 燃料の最大値 </summary>
	public static readonly float Max_Fuel = 100;

	[Tooltip("燃料消費量")]
	[SerializeField] float fuelConsumption = 1;

	//加速/衝突効果
	public GameObject paticlePrefab;

	void Start()
	{
		ui.AccelerateButton.onClick.AddListener(Accelerate);
		initForwordMoveSpeed = addForwordMoveSpeed;
		initVerticalAndHorizontalMoveSpeed = addVerticalAndHorizontalMoveSpeed;
		paticlePrefab.SetActive(false);
	}

	void Update()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			return;
		}

		if (ui.IsFade == false)
		{
			return;
		}

		if (gameManager.IsPlay == false)
		{
			return;
		}

		float joystickHorizontal = ui.FloatingJoystick.Horizontal;
		float joystickVertical = ui.FloatingJoystick.Vertical;

		//上下回転
		if (0.1f < joystickVertical)
		{
			if (planePrefab.transform.localEulerAngles.x < 31 || planePrefab.transform.localEulerAngles.x > 330)
			{
				planePrefab.transform.Rotate(-verticalRotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}
		else if (joystickVertical < -0.1f)
		{
			if (planePrefab.transform.localEulerAngles.x < 30 || planePrefab.transform.localEulerAngles.x > 329)
			{
				planePrefab.transform.Rotate(verticalRotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}

		//左右回転
		if (0.1f < joystickHorizontal)
		{
			if (planePrefab.transform.localEulerAngles.z < 31 || planePrefab.transform.localEulerAngles.z > 330)
			{
				planePrefab.transform.Rotate(0, 0, -horizontalRotateSpeed * Time.deltaTime, Space.World);
			}
		}
		else if (joystickHorizontal < -0.1f)
		{
			if (planePrefab.transform.localEulerAngles.z < 30 || planePrefab.transform.localEulerAngles.z > 329)
			{
				planePrefab.transform.Rotate(0, 0, horizontalRotateSpeed * Time.deltaTime, Space.World);
			}
		}

		//y軸を元に戻す処理
		if (0 < planePrefab.transform.rotation.y)
		{
			planePrefab.transform.Rotate(0, -yRotateSpeed * Time.deltaTime, 0, Space.World);
		}
		if (planePrefab.transform.rotation.y < 0)
		{
			planePrefab.transform.Rotate(0, yRotateSpeed * Time.deltaTime, 0, Space.World);
		}

		//回転軸を元に戻す処理
		if (joystickVertical == 0.0f)
		{
			if (0 < planePrefab.transform.rotation.x)
			{
				planePrefab.transform.Rotate(-verticalRotateSpeed * Time.deltaTime, 0, 0);
			}
			if (planePrefab.transform.rotation.x < 0)
			{
				planePrefab.transform.Rotate(verticalRotateSpeed * Time.deltaTime, 0, 0);
			}
		}
		if (joystickHorizontal == 0.0f)
		{
			if (0 < planePrefab.transform.rotation.z)
			{
				planePrefab.transform.Rotate(0, 0, -horizontalRotateSpeed * Time.deltaTime);
			}
			if (planePrefab.transform.rotation.z < 0)
			{
				planePrefab.transform.Rotate(0, 0, horizontalRotateSpeed * Time.deltaTime);
			}
		}

		Accelerate();
	}

	/// <summary>
	/// 加速
	/// </summary>
	void Accelerate()
	{
		if (ui.ButtonDownFlag == true && 0 < currentFuel)
		{
			paticlePrefab.SetActive(true);
			ChangeForwordMoveSpeed(changeForwordMovepeed);
			ChangeVerticalAndHorizontalMoveSpeed(changeVerticalAndHorizontalMoveSpeed);
			currentFuel = currentFuel - fuelConsumption;
		}
		else
		{
			paticlePrefab.SetActive(false);
			ChangeForwordMoveSpeed(changeForwordMovepeed * -0.5f);
			ChangeVerticalAndHorizontalMoveSpeed(-changeVerticalAndHorizontalMoveSpeed);
		}
	}

	void FixedUpdate()
	{
		if (StageManager.SingletonInstance.IsSceneSwitched == true)
		{
			rb.velocity = Vector3.zero;
			return;
		}

		if (ui.IsFade == false)
		{
			rb.velocity = Vector3.zero;
			return;
		}

		if (gameManager.IsPlay == false)
		{
			rb.velocity = Vector3.zero;
			return;
		}

		// 移動は Rigidbody の速度で制御する
		float joystickHorizontal = ui.FloatingJoystick.Horizontal;
		float joystickVertical = ui.FloatingJoystick.Vertical;

		float horizontal = Mathf.Clamp(joystickHorizontal, -1f, 1f);
		float vertical = Mathf.Clamp(joystickVertical, -1f, 1f);

		Vector3 velocity = Vector3.zero;
		velocity = velocity + this.transform.forward * addForwordMoveSpeed; // 自動前進
		velocity = velocity + Vector3.up * (vertical * addVerticalAndHorizontalMoveSpeed * 0.5f); // 上下移動（Y軸）
		velocity = velocity + this.transform.right * (horizontal * addVerticalAndHorizontalMoveSpeed); // 左右（A/D）
		rb.velocity = velocity;

		// 位置を範囲内にクランプ（ワールド座標の X/Y）
		Vector3 pos = rb.position;
		pos.x = Mathf.Clamp(pos.x, horizontalMin, horizontalMax);
		pos.y = Mathf.Clamp(pos.y, verticalMin, verticalMax);
		rb.position = pos;
	}

	//自動前進スピードを徐々に変える
	void ChangeForwordMoveSpeed(float value)
	{
		addForwordMoveSpeed = addForwordMoveSpeed + value;

		if (initForwordMoveSpeed * 5 <= addForwordMoveSpeed)
		{
			addForwordMoveSpeed = initForwordMoveSpeed * 5;
		}

		if (addForwordMoveSpeed <= initForwordMoveSpeed)
		{
			addForwordMoveSpeed = initForwordMoveSpeed;
		}
	}

	//上下左右の移動スピードを徐々に変える
	void ChangeVerticalAndHorizontalMoveSpeed(float value)
	{
		addVerticalAndHorizontalMoveSpeed = addVerticalAndHorizontalMoveSpeed + value;

		if (initVerticalAndHorizontalMoveSpeed * 2 <= addVerticalAndHorizontalMoveSpeed)
		{
			addVerticalAndHorizontalMoveSpeed = initVerticalAndHorizontalMoveSpeed * 2;
		}

		if (addVerticalAndHorizontalMoveSpeed <= initVerticalAndHorizontalMoveSpeed)
		{
			addVerticalAndHorizontalMoveSpeed = initVerticalAndHorizontalMoveSpeed;
		}
	}

	/// <summary>
	/// 燃料の追加
	/// </summary>
	/// <param name="value">追加量</param>
	void AddFuel(float value)
	{
		currentFuel = currentFuel + value;
		if (Max_Fuel <= currentFuel)
		{
			currentFuel = Max_Fuel;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Coin")
		{
			gameManager.AddCoin(other.GetComponent<Coin>().Value);
		}

		if (other.tag == "Fuel")
		{
			AddFuel(other.GetComponent<Fuel>().Value);
		}

		if (other.tag == "Timer")
		{
			gameManager.AddTimer(other.GetComponent<Timer>().Value);
		}
	}

	/// <summary>
	/// プレイヤーの位置をリセットする
	/// </summary>
	public void ResetPlayerPosition()
	{
		rb.velocity = Vector3.zero;
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		planePrefab.transform.localRotation = Quaternion.identity;
	}

	void OnGUI()
	{
#if UNITY_EDITOR//Unityエディター上での処理

		GUIStyle styleGreen = new GUIStyle();
		styleGreen.fontSize = 30;
		GUIStyleState styleStateGreen = new GUIStyleState();
		styleStateGreen.textColor = Color.green;
		styleGreen.normal = styleStateGreen;

		GUIStyle styleRed = new GUIStyle();
		styleRed.fontSize = 30;
		GUIStyleState styleStateRed = new GUIStyleState();
		styleStateRed.textColor = Color.red;
		styleRed.normal = styleStateRed;

		GUIStyle styleBlack = new GUIStyle();
		styleBlack.fontSize = 30;
		GUIStyleState styleStateBlack = new GUIStyleState();
		styleStateBlack.textColor = Color.black;
		styleBlack.normal = styleStateBlack;

		GUIStyle styleYellow = new GUIStyle();
		styleYellow.fontSize = 30;
		GUIStyleState styleStateYellow = new GUIStyleState();
		styleStateYellow.textColor = Color.yellow;
		styleYellow.normal = styleStateYellow;

		int lineHeight = 50;

		float joystickHorizontal = ui.FloatingJoystick.Horizontal;
		float joystickVertical = ui.FloatingJoystick.Vertical;
		GUI.Box(new Rect(10, 0 * lineHeight, 100, 50), "inputHorizontal", styleRed);
		GUI.Box(new Rect(350, 0 * lineHeight, 100, 50), joystickHorizontal.ToString(), styleRed);
		GUI.Box(new Rect(10, 1 * lineHeight, 100, 50), "inputVertical", styleRed);
		GUI.Box(new Rect(350, 1 * lineHeight, 100, 50), joystickVertical.ToString(), styleRed);
#endif //終了  
	}
}