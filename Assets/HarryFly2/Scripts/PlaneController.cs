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
	[SerializeField] float rotateSpeed;
	[Tooltip("機体元に戻す回転速度")]
	[SerializeField] float returenRotateSpeed;
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
		initialFMSpeed = forwordMoveSpeed;
		initialMSpeed = moveSpeed;

		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
	}

	void Update()
	{
		float joystickHorizontal = UI.SingletonInstance.FloatingJoystick.Horizontal;
		float joystickVertical = UI.SingletonInstance.FloatingJoystick.Vertical;
		Debug.Log("横の移動量 : " + joystickHorizontal);
		Debug.Log("縦の移動量 : " + joystickVertical);

		//上下左右回転
		if (Input.GetKey(KeyCode.W) || 0.1f < joystickVertical)
		{
			planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
		}
		else if (Input.GetKey(KeyCode.S) || joystickVertical < -0.1f)
		{
			planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
		}

		if (Input.GetKey(KeyCode.D) || 0.1f < joystickHorizontal)
		{
			planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
		}
		else if (Input.GetKey(KeyCode.A) || joystickHorizontal < -0.1f)
		{
			planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
		}


		if (0 < planePrefab.transform.rotation.y)
		{
			planePrefab.transform.Rotate(0, -rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, Space.World);
		}
		if (planePrefab.transform.rotation.y < 0)
		{
			planePrefab.transform.Rotate(0, rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, Space.World);
		}

		//回転軸をもとに戻す処理
		if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
		{
			if (0 < planePrefab.transform.rotation.x)
			{
				planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
			}
			if (planePrefab.transform.rotation.x < 0)
			{
				planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
			}
		}
		if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
		{
			if (0 < planePrefab.transform.rotation.z)
			{
				planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed);
			}
			if (planePrefab.transform.rotation.z < 0)
			{
				planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed);
			}
		}

		//加速
		if (Input.GetKey(KeyCode.Space) && 0 < currentFuel)
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

		float keyHorizontal = 0f;
		if (Input.GetKey(KeyCode.D)) keyHorizontal += 1f;
		if (Input.GetKey(KeyCode.A)) keyHorizontal -= 1f;
		float keyVertical = 0f;
		if (Input.GetKey(KeyCode.W)) keyVertical += 1f;
		if (Input.GetKey(KeyCode.S)) keyVertical -= 1f;

		float horizontal = Mathf.Clamp(joystickHorizontal + keyHorizontal, -1f, 1f);
		float vertical = Mathf.Clamp(joystickVertical + keyVertical, -1f, 1f);

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
		GUI.Box(new Rect(10, 0 * lineHeight, 100, 50), "inputHorizontal", styleGreen);
		GUI.Box(new Rect(350, 0 * lineHeight, 100, 50), joystickHorizontal.ToString(), styleGreen);
		GUI.Box(new Rect(10, 1 * lineHeight, 100, 50), "inputVertical", styleGreen);
		GUI.Box(new Rect(350, 1 * lineHeight, 100, 50), joystickVertical.ToString(), styleGreen);
		GUI.Box(new Rect(10, 5 * lineHeight, 100, 50), "rb.velocity", styleGreen);
		GUI.Box(new Rect(350, 5 * lineHeight, 100, 50), rb.velocity.ToString(), styleGreen);
#endif //終了  
	}
}