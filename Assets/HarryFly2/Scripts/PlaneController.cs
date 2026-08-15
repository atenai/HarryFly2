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
	[SerializeField] GameObject[] planePrefabs;
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
	[Tooltip("通常時と加速時の自動前進速度を徐々に変える値（1秒あたり）。60fpsで1フレームにつき2変えていたときと同じ速さになる値を入れてある")]
	float changeForwordMoveSpeedPerSecond = 120f;
	[Tooltip("通常時と加速時の上下左右移動速度を徐々に変える値（1秒あたり）。60fpsで1フレームにつき1変えていたときと同じ速さになる値を入れてある")]
	float changeVerticalAndHorizontalMoveSpeedPerSecond = 60f;

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

	[Tooltip("1秒あたりの燃料消費量。60fpsで1フレームにつき1消費していたときと同じ速さになる値を入れてある")]
	[SerializeField] float fuelConsumptionPerSecond = 60;

	//加速/衝突効果
	public GameObject paticlePrefab;

	/// <summary>ゴール済みかどうか。リザルトを二重に出さないための判定に使う</summary>
	bool hasGoaled = false;

	void Start()
	{
		initForwordMoveSpeed = addForwordMoveSpeed;
		initVerticalAndHorizontalMoveSpeed = addVerticalAndHorizontalMoveSpeed;
		paticlePrefab.SetActive(false);
		ChangePlaneModel();
	}

	/// <summary>
	/// 指定番号の機体モデルが設定されているか。
	/// ショップの枠は6個先に用意してあるので、モデル未設定のスロットを
	/// 選ばせないための判定に使う
	/// </summary>
	/// <param name="index">機体スロット番号</param>
	public bool HasModel(int index)
	{
		return 0 <= index && index < planePrefabs.Length && planePrefabs[index] != null;
	}

	/// <summary>ショップに並ぶ機体スロット数</summary>
	public int ModelCount => planePrefabs.Length;

	void ChangePlaneModel()
	{
		foreach (var model in planePrefabs)
		{
			// モデル未設定のスロットは空のままなので飛ばす
			if (model != null)
			{
				model.SetActive(false);
			}
		}

		int index = ShopManager.SingletonInstance.PlaneModelNumber;
		if (HasModel(index) == false)
		{
			// 念のため。未設定スロットが選ばれていたら0番に戻す
			index = 0;
		}
		if (HasModel(index) == true)
		{
			planePrefabs[index].SetActive(true);
		}
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

		if (ui.Panel_Shop.gameObject.activeSelf == true)
		{
			ChangePlaneModel();
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
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.x < 31 || planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.x > 330)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(-verticalRotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}
		else if (joystickVertical < -0.1f)
		{
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.x < 30 || planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.x > 329)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(verticalRotateSpeed * Time.deltaTime, 0, 0, Space.World);
			}
		}

		//左右回転
		if (0.1f < joystickHorizontal)
		{
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.z < 31 || planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.z > 330)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, 0, -horizontalRotateSpeed * Time.deltaTime, Space.World);
			}
		}
		else if (joystickHorizontal < -0.1f)
		{
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.z < 30 || planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localEulerAngles.z > 329)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, 0, horizontalRotateSpeed * Time.deltaTime, Space.World);
			}
		}

		//y軸を元に戻す処理
		if (0 < planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.y)
		{
			planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, -yRotateSpeed * Time.deltaTime, 0, Space.World);
		}
		if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.y < 0)
		{
			planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, yRotateSpeed * Time.deltaTime, 0, Space.World);
		}

		//回転軸を元に戻す処理
		if (joystickVertical == 0.0f)
		{
			if (0 < planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.x)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(-verticalRotateSpeed * Time.deltaTime, 0, 0);
			}
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.x < 0)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(verticalRotateSpeed * Time.deltaTime, 0, 0);
			}
		}
		if (joystickHorizontal == 0.0f)
		{
			if (0 < planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.z)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, 0, -horizontalRotateSpeed * Time.deltaTime);
			}
			if (planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.rotation.z < 0)
			{
				planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.Rotate(0, 0, horizontalRotateSpeed * Time.deltaTime);
			}
		}

		Accelerate();
	}

	/// <summary>
	/// 加速。
	/// Update から毎フレーム呼ぶ。加速ボタンの onClick には登録しないこと。
	/// onClick は指を離したときに発火するが、EventSystem は PointerUp を PointerClick より
	/// 先に配信するので、その時点では既に ButtonDownFlag が false になっている。
	/// つまり onClick 経由の呼び出しは必ず減速側の分岐に落ちて、そのフレームだけ減速が余分に1回走る
	/// （EventSystem と本スクリプトの実行順は未定義なので、そのフレームが
	/// 　「減速2回」になるか「加速1回＋減速1回」になるかは不定）
	/// </summary>
	void Accelerate()
	{
		if (ui.ButtonDownFlag == true && 0 < currentFuel)
		{
			paticlePrefab.SetActive(true);
			ChangeForwordMoveSpeed(changeForwordMoveSpeedPerSecond * Time.deltaTime);
			ChangeVerticalAndHorizontalMoveSpeed(changeVerticalAndHorizontalMoveSpeedPerSecond * Time.deltaTime);
			// フレームレートで燃費が変わらないように、経過時間で消費量を決める
			currentFuel = currentFuel - fuelConsumptionPerSecond * Time.deltaTime;
			// 処理落ちで1フレームが長くなると大きくマイナスに振れる。
			// そのままだと次に燃料を拾ったときの回復量がマイナス分だけ目減りするので下限を切る
			if (currentFuel < 0)
			{
				currentFuel = 0;
			}
			// ブースト中は振動させない。
			// 鳴らし続けると、アイテムを取ったときの単発振動がその中に埋もれて感じ取れなくなる
		}
		else
		{
			paticlePrefab.SetActive(false);
			ChangeForwordMoveSpeed(changeForwordMoveSpeedPerSecond * -0.5f * Time.deltaTime);
			ChangeVerticalAndHorizontalMoveSpeed(-changeVerticalAndHorizontalMoveSpeedPerSecond * Time.deltaTime);
		}
	}

	void FixedUpdate()
	{
		// ゴール後はキネマティックにして固定してある。
		// ここで velocity を触ると「キネマティックな剛体に速度は設定できない」警告が毎フレーム出る
		if (hasGoaled == true)
		{
			return;
		}

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

	void OnTriggerEnter(Collider collider)
	{
		if (collider.CompareTag("Coin") == true)
		{
			gameManager.AddCoin(collider.GetComponent<Coin>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Light);
		}

		if (collider.CompareTag("Fuel") == true)
		{
			AddFuel(collider.GetComponent<Fuel>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Medium);
		}

		if (collider.CompareTag("Timer") == true)
		{
			gameManager.AddTimer(collider.GetComponent<Timer>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Medium);
		}

		if (collider.CompareTag("Goal") == true)
		{
			// ゴールの当たり判定が複数あっても、リザルトを二重に出さない
			if (hasGoaled == true)
			{
				return;
			}
			hasGoaled = true;

			Debug.Log("ゴール！");
			HapticFeedback.Play(HapticFeedback.Strength.Heavy);

			// ゴール報酬：このステージで拾ったコインを2倍にする。
			// ApplyGoalBonus を呼ぶと加算後になるので、拾った数は先に控えておく
			int stageCoin = gameManager.StageCoinCount;
			int bonusCoin = gameManager.ApplyGoalBonus();
			Debug.Log("ゴールボーナス：+" + bonusCoin);

			// ブーストの炎はここで消す。IsPlay を false にすると Update が Accelerate() まで
			// 到達しなくなるので、消灯処理が走らないまま出っぱなしになってしまう
			paticlePrefab.SetActive(false);

			// 操作と制限時間を止める。止めないとリザルトを見ている間に時間切れになる
			gameManager.IsPlay = false;
			FreezeForGoal();

			// ステージが切り替わる前にコインを保存する
			gameManager.SaveCoin();

			// シーンの切り替えはリザルトの「NEXT」を押してから。
			// ゴールした瞬間に切り替えると、次ステージは先読み済みなので次のフレームで
			// シーンが変わってしまい、ゴール文字が映らないまま消えていた
			ui.ShowResult(gameManager.PlayTime, stageCoin, bonusCoin, GoToNextStage);
		}
	}

	/// <summary>
	/// ゴール後に機体を完全に止める。
	/// 速度を0にするだけでは足りない。ゴール枠や建物に接触した状態だと、
	/// 物理側のめり込み解消で機体が押し出され続け、カメラの外まで飛んでいってしまう。
	/// キネマティックにすると押し出しも回転も起きなくなる
	/// </summary>
	void FreezeForGoal()
	{
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.isKinematic = true;
	}

	/// <summary>
	/// リザルトの「NEXT」から呼ばれる。ここで初めて次のステージへ切り替える
	/// </summary>
	void GoToNextStage()
	{
		if (AdsManager.SingletonInstance != null)
		{
			// リザルトを見せてから出す。ゴールと同時に出すとリザルトが広告で隠れてしまう
			AdsManager.SingletonInstance.ShowAdsInterstitialCount();
		}

		ui.HideResult();
		// 次ステージのロードが終わるまでシーンは切り替わらない。
		// その間プレイヤーが映ってしまうので、障害物に当たったときと同様に画面を隠す
		ui.FadeIn();
		StageManager.SingletonInstance.IsTriggered = true;
	}

	void OnCollisionEnter(Collision collision)
	{
		// ゴール後はその場で止まっているだけなので、接触してもステージを切り替えない
		if (hasGoaled == true)
		{
			return;
		}

		if (collision.gameObject.CompareTag("Obstacle") == true)
		{
			Debug.Log("障害物に衝突した");
			HapticFeedback.Play(HapticFeedback.Strength.VeryHeavy);
			AdsManager.SingletonInstance.ShowAdsInterstitialCount();
			ResetPlayerPosition();
			// ステージが切り替わる前にコインを保存する
			gameManager.SaveCoin();
			// シーンを切り替える
			StageManager.SingletonInstance.IsTriggered = true;
			ui.FadeIn();
		}
	}

	/// <summary>
	/// プレイヤーの位置をリセットする
	/// </summary>
	void ResetPlayerPosition()
	{
		rb.velocity = Vector3.zero;
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		planePrefabs[ShopManager.SingletonInstance.PlaneModelNumber].transform.localRotation = Quaternion.identity;
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