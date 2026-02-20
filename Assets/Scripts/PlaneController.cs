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
	}

	void Update()
	{
		//自動前進
		this.transform.Translate(forwordMoveSpeed * Time.deltaTime, 0, 0);

		//上下左右移動
		if (Input.GetKey(KeyCode.W))
		{
			this.transform.Translate(0, moveSpeed * Time.deltaTime * 0.5f, 0);
			if (planePrefab.transform.localEulerAngles.z < 30 || planePrefab.transform.localEulerAngles.z > 329)
			{
				//回転させる
				if (planePrefab.transform.rotation.z > 0)
				{
					planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime, Space.World);
				}
				else
				{
					planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
				}
			}
		}

		if (Input.GetKey(KeyCode.S))
		{
			this.transform.Translate(0, -moveSpeed * Time.deltaTime * 0.5f, 0);
			if (planePrefab.transform.localEulerAngles.z < 31 || planePrefab.transform.localEulerAngles.z > 330)
			{
				if (planePrefab.transform.rotation.z < 0)
				{
					planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime, Space.World);
				}
				else
				{
					planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
				}
			}
		}

		if (Input.GetKey(KeyCode.D))
		{
			this.transform.Translate(0, 0, -moveSpeed * Time.deltaTime);
			if (planePrefab.transform.localEulerAngles.x < 31 || planePrefab.transform.localEulerAngles.x > 330)
			{
				if (planePrefab.transform.rotation.x < 0)
				{
					planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime, 0, 0, Space.World);
				}
				else
				{
					planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
				}
			}
		}

		if (Input.GetKey(KeyCode.A))
		{
			this.transform.Translate(0, 0, moveSpeed * Time.deltaTime);
			if (planePrefab.transform.localEulerAngles.x < 30 || planePrefab.transform.localEulerAngles.x > 329)
			{
				if (planePrefab.transform.rotation.x > 0)
				{
					planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime, 0, 0, Space.World);
				}
				else
				{
					planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
				}
			}
		}


		if (planePrefab.transform.rotation.y > 0)
		{
			planePrefab.transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0 * returenRotateSpeed);
		}
		if (planePrefab.transform.rotation.y < 0)
		{
			planePrefab.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0 * returenRotateSpeed);
		}

		//回転軸をもとに戻す処理
		if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
		{

			if (planePrefab.transform.rotation.z > 0)
			{
				planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed);
			}
			if (planePrefab.transform.rotation.z < 0)
			{
				planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed);
			}

		}
		if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
		{
			if (planePrefab.transform.rotation.x > 0)
			{
				planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
			}
			if (planePrefab.transform.rotation.x < 0)
			{
				planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
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
		mainCamera.transform.Translate(value, 0, 0, Space.World);

		if (mainCamera.transform.localPosition.x >= -2.5f)
		{
			mainCamera.transform.localPosition = new Vector3(-2.5f, 0.5f, 0);
		}

		if (mainCamera.transform.localPosition.x <= -7f)
		{
			mainCamera.transform.localPosition = new Vector3(-7f, 0.5f, 0);
		}
	}

	/// <summary>
	/// 燃料の追加
	/// </summary>
	/// <param name="value">追加量</param>
	public void AddFuel(float value)
	{
		currentFuel = currentFuel + value;
	}
}
