using UnityEngine;

/// <summary>
/// 飛行機コントローラー
/// </summary>
public class PlaneController : MonoBehaviour
{
	private static PlaneController singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static PlaneController SingletonInstance => singletonInstance;

	[Tooltip("飛行機のモデル")]
	[SerializeField] GameObject planePrefab;
	[Tooltip("メインカメラ")]
	[SerializeField] GameObject mainCamera;
	[Tooltip("リジッドボディ")]
	[SerializeField] Rigidbody rb;

	[Tooltip("自動前進速度")]
	[SerializeField] float forwordMoveSpeed;
	[Tooltip("上下左右移動速度")]
	[SerializeField] float moveSpeed;
	public float initialFMSpeed;
	public float initialMSpeed;
	[Tooltip("機体回転速度")]
	float rotateSpeed = 40;
	private float cameraSpeed = 3.5f;
	private float changeFWSpeed = 2f;
	private float changeMSpeed = 1f;

	/// <summary>
	/// 現在の燃料
	/// </summary>
	float currentFuel = 100f;
	public float CurrentFuel => currentFuel;

	/// <summary>
	/// 燃料の最大値
	/// </summary>
	float maxFuel = 100;
	public float MaxFuel => maxFuel;

	[Tooltip("燃料消費量")]
	[SerializeField] float fuelConsumption = 1;

	//加速/衝突効果
	public GameObject paticlePrefab;
	public GameObject boom;

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
		UI.SingletonInstance.AccelerateButton.onClick.AddListener(Accelerate);
		initialFMSpeed = forwordMoveSpeed;
		initialMSpeed = moveSpeed;
	}

	void Update()
	{
		float joystickHorizontal = UI.SingletonInstance.FloatingJoystick.Horizontal;
		float joystickVertical = UI.SingletonInstance.FloatingJoystick.Vertical;

		//上下回転
		if (0.1f < joystickVertical)
		{
			if (planePrefab.transform.localEulerAngles.x < 31 || planePrefab.transform.localEulerAngles.x > 330)
			{
				planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}
		else if (joystickVertical < -0.1f)
		{
			if (planePrefab.transform.localEulerAngles.x < 30 || planePrefab.transform.localEulerAngles.x > 329)
			{
				planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}

		//左右回転
		if (0.1f < joystickHorizontal)
		{
			if (planePrefab.transform.localEulerAngles.z < 31 || planePrefab.transform.localEulerAngles.z > 330)
			{
				planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime, Space.World);
			}
		}
		else if (joystickHorizontal < -0.1f)
		{
			if (planePrefab.transform.localEulerAngles.z < 30 || planePrefab.transform.localEulerAngles.z > 329)
			{
				planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime, Space.World);
			}
		}

		//y軸を元に戻す処理
		if (0 < planePrefab.transform.rotation.y)
		{
			planePrefab.transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0, Space.World);
		}
		if (planePrefab.transform.rotation.y < 0)
		{
			planePrefab.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);
		}

		//回転軸を元に戻す処理
		if (joystickVertical == 0.0f)
		{
			if (0 < planePrefab.transform.rotation.x)
			{
				planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime, 0, 0);
			}
			if (planePrefab.transform.rotation.x < 0)
			{
				planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime, 0, 0);
			}
		}
		if (joystickHorizontal == 0.0f)
		{
			if (0 < planePrefab.transform.rotation.z)
			{
				planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
			}
			if (planePrefab.transform.rotation.z < 0)
			{
				planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
			}
		}

		Accelerate();
	}

	/// <summary>
	/// 加速
	/// </summary>
	void Accelerate()
	{
		if (UI.SingletonInstance.ButtonDownFlag == true && 0 < currentFuel)
		{
			paticlePrefab.SetActive(true);
			ChangeFMSpeed(changeFWSpeed);
			ChangeMSpeed(changeMSpeed);
			ChangeXOfCamera(-cameraSpeed * Time.deltaTime);
			currentFuel = currentFuel - fuelConsumption;
		}
		else
		{
			paticlePrefab.SetActive(false);
			ChangeFMSpeed(-0.5f * changeFWSpeed);
			ChangeMSpeed(-changeMSpeed);
			ChangeXOfCamera(cameraSpeed * Time.deltaTime * 2);
		}
	}

	void FixedUpdate()
	{
		// 移動は Rigidbody の速度で制御する
		float joystickHorizontal = UI.SingletonInstance.FloatingJoystick.Horizontal;
		float joystickVertical = UI.SingletonInstance.FloatingJoystick.Vertical;

		float horizontal = Mathf.Clamp(joystickHorizontal, -1f, 1f);
		float vertical = Mathf.Clamp(joystickVertical, -1f, 1f);

		Vector3 vel = Vector3.zero;
		vel += transform.forward * forwordMoveSpeed; // 自動前進
		vel += Vector3.up * (vertical * moveSpeed * 0.5f); // 上下移動（Y軸）
		vel += transform.right * (horizontal * moveSpeed); // 左右（A/D）

		if (rb != null)
		{
			rb.velocity = vel;
		}
	}

	//加速
	public void ChangeFMSpeed(float value)
	{
		forwordMoveSpeed += value;
		if (forwordMoveSpeed >= initialFMSpeed * 5)
		{
			forwordMoveSpeed = initialFMSpeed * 5;
		}
		if (forwordMoveSpeed <= initialFMSpeed)
		{
			forwordMoveSpeed = initialFMSpeed;
		}
	}

	//左右のスピードを変える
	public void ChangeMSpeed(float value)
	{
		moveSpeed += value;
		if (moveSpeed >= initialMSpeed * 2)
		{
			moveSpeed = initialMSpeed * 2;
		}
		if (moveSpeed <= initialMSpeed)
		{
			moveSpeed = initialMSpeed;
		}
	}

	//加速時のカメラと機体の距離を変更する
	public void ChangeXOfCamera(float value)
	{
		// mainCamera.transform.Translate(value, 0, 0, Space.World);

		// if (mainCamera.transform.localPosition.x >= -2.5f)
		// {
		// 	mainCamera.transform.localPosition = new Vector3(-2.5f, 0.5f, 0);
		// }

		// if (mainCamera.transform.localPosition.x <= -7f)
		// {
		// 	mainCamera.transform.localPosition = new Vector3(-7f, 0.5f, 0);
		// }
	}

	/// <summary>
	/// 燃料の追加
	/// </summary>
	/// <param name="value">追加量</param>
	public void AddFuel(float value)
	{
		currentFuel = currentFuel + value;
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

		float joystickHorizontal = UI.SingletonInstance.FloatingJoystick.Horizontal;
		float joystickVertical = UI.SingletonInstance.FloatingJoystick.Vertical;
		GUI.Box(new Rect(10, 0 * lineHeight, 100, 50), "inputHorizontal", styleRed);
		GUI.Box(new Rect(350, 0 * lineHeight, 100, 50), joystickHorizontal.ToString(), styleRed);
		GUI.Box(new Rect(10, 1 * lineHeight, 100, 50), "inputVertical", styleRed);
		GUI.Box(new Rect(350, 1 * lineHeight, 100, 50), joystickVertical.ToString(), styleRed);
#endif //終了  
	}
}