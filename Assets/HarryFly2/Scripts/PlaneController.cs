using System.Collections;
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

	/// <summary>上下左右の移動速度の基準。機体ごとの倍率はここに掛ける</summary>
	public const float Base_VerticalAndHorizontal_MoveSpeed = 200f;
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

	/// <summary>
	/// 機体1体分の性能。ショップで選んだ機体によって操作感を変える
	/// </summary>
	[System.Serializable]
	public class PlaneSpec
	{
		[Tooltip("機体の呼び名（ショップの表示や識別用）")]
		public string displayName = "UAV";

		[Tooltip("上下左右の移動速度の倍率。大きいほど機敏に動く")]
		public float moveSpeedMultiplier = 1f;

		[Tooltip("ブースト容量（燃料の最大値）の倍率。大きいほど長くブーストできる")]
		public float fuelCapacityMultiplier = 1f;

		[Tooltip("当たり判定の大きさの倍率。小さいほど障害物に当たりにくい")]
		public float colliderScaleMultiplier = 1f;
	}

	[Header("機体ごとの性能")]
	[Tooltip("planePrefabs と同じ並び順で、機体ごとの性能を設定する")]
	[SerializeField]
	PlaneSpec[] planeSpecs = new PlaneSpec[6];

	/// <summary>この機体の燃料の最大値。機体ごとに変わるので定数ではなく実行時の値を持つ</summary>
	float maxFuel = Base_Max_Fuel;
	public float MaxFuel => maxFuel;

	/// <summary>燃料の最大値の基準。ここに機体ごとの倍率を掛ける</summary>
	public const float Base_Max_Fuel = 100f;

	/// <summary>性能を適用済みの機体番号。毎フレーム適用し直さないための比較用</summary>
	int appliedSpecIndex = -1;

	/// <summary>当たり判定の元の大きさ。倍率はここに掛ける</summary>
	Vector3 baseColliderSize = Vector3.one;
	bool hasBaseColliderSize = false;

	/// <summary> 燃料の最大値 </summary>

	[Tooltip("1秒あたりの燃料消費量。60fpsで1フレームにつき1消費していたときと同じ速さになる値を入れてある")]
	[SerializeField] float fuelConsumptionPerSecond = 60;

	//加速/衝突効果
	public GameObject paticlePrefab;

	[Header("障害物に衝突したときの爆発")]
	[Tooltip("衝突地点に出す爆発エフェクト")]
	[SerializeField] GameObject explosionPrefab;

	[Tooltip("爆発の大きさ。衝突地点はカメラのすぐ前なので、大きすぎると画面を覆ってしまう")]
	[SerializeField] float explosionScale = 2.5f;

	[Tooltip("爆発を見せてから次のステージへ切り替えるまでの時間（秒）。長くするとテンポが悪くなる")]
	[SerializeField] float explosionViewSeconds = 1.0f;

	[Tooltip("衝突時の爆発音")]
	[SerializeField] AudioClip explosionSound;

	[Tooltip("爆発音の音量")]
	[SerializeField, Range(0f, 1f)] float explosionVolume = 0.8f;

	[Tooltip("衝突地点に出す衝撃波のリング。爆発だけだと衝撃の広がりが出ない")]
	[SerializeField] GameObject shockwavePrefab;

	[Tooltip("衝撃波の大きさ。爆発より大きく開かないと衝撃波に見えない")]
	[SerializeField] float shockwaveScale = 1.5f;

	/// <summary>出した爆発を消すまでの時間（秒）。シーンが切り替われば一緒に消えるが、その保険</summary>
	const float Explosion_Lifetime_Seconds = 5f;

	[Header("アイテム取得エフェクト")]
	[Tooltip("コインを取ったときに出すエフェクト")]
	[SerializeField] GameObject coinPickupEffect;

	[Tooltip("燃料を取ったときに出すエフェクト")]
	[SerializeField] GameObject fuelPickupEffect;

	[Tooltip("時間を取ったときに出すエフェクト")]
	[SerializeField] GameObject timerPickupEffect;

	/// <summary>
	/// 取得エフェクトの大きさはアイテムごとに分ける。
	/// エフェクトによって元の作りの大きさが違い、同じ倍率だと
	/// コインの閃光は映えるのに燃料と時間は埋もれてしまう
	/// </summary>
	[Tooltip("コイン取得エフェクトの大きさ")]
	[SerializeField] float coinPickupEffectScale = 0.2f;

	[Tooltip("燃料取得エフェクトの大きさ")]
	[SerializeField] float fuelPickupEffectScale = 0.5f;

	[Tooltip("時間取得エフェクトの大きさ")]
	[SerializeField] float timerPickupEffectScale = 0.5f;

	[Tooltip("取得エフェクトを出す位置（機体から見た相対位置）。機体の中に埋めると自機に隠れて見えない")]
	[SerializeField] Vector3 pickupEffectOffset = new Vector3(0f, 0f, 1.5f);

	/// <summary>
	/// 取得エフェクトを消すまでの時間（秒）。
	/// エフェクト側にも自動消滅の設定があるが、取り切れなかったときの保険
	/// </summary>
	const float Pickup_Effect_Lifetime_Seconds = 3f;

	/// <summary>
	/// ブースト中かどうか。
	/// 判定は Accelerate() の中にしかなく、噴射トレイルなど外の演出から参照できなかったので公開する。
	/// ゴール後・衝突後は Update が Accelerate() まで到達しないため、
	/// その場合は最後に立てた値のまま残る。演出を止める側で別途止めること
	/// </summary>
	bool isBoosting = false;
	public bool IsBoosting => isBoosting;

	/// <summary>ゴール済みかどうか。リザルトを二重に出さないための判定に使う</summary>
	bool hasGoaled = false;

	/// <summary>衝突済みかどうか。爆発とステージ切り替えを二重に走らせないための判定に使う</summary>
	bool hasCrashed = false;

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

		ApplyPlaneSpec(index);
	}

	/// <summary>
	/// 選んだ機体の性能を反映する。
	/// ショップを開いている間は毎フレーム呼ばれるので、
	/// 機体が変わったときだけ処理する（毎回燃料を満タンに戻してしまわないため）
	/// </summary>
	/// <param name="index">機体スロット番号</param>
	void ApplyPlaneSpec(int index)
	{
		if (appliedSpecIndex == index)
		{
			return;
		}
		appliedSpecIndex = index;

		PlaneSpec spec = GetPlaneSpec(index);

		// 上下左右の速度。ブースト中の加速はこの値を基準に増減するので、基準値ごと差し替える
		initVerticalAndHorizontalMoveSpeed = Base_VerticalAndHorizontal_MoveSpeed * spec.moveSpeedMultiplier;
		addVerticalAndHorizontalMoveSpeed = initVerticalAndHorizontalMoveSpeed;

		// ブースト容量。機体を変えた時点で満タンにする
		maxFuel = Base_Max_Fuel * spec.fuelCapacityMultiplier;
		currentFuel = maxFuel;

		ApplyColliderScale(spec.colliderScaleMultiplier);
	}

	/// <summary>
	/// 当たり判定の大きさを変える。見た目の大きさは変えず、判定だけを変える
	/// </summary>
	/// <param name="multiplier">元の大きさに対する倍率</param>
	void ApplyColliderScale(float multiplier)
	{
		BoxCollider box = GetComponent<BoxCollider>();
		if (box == null)
		{
			return;
		}

		if (hasBaseColliderSize == false)
		{
			baseColliderSize = box.size;
			hasBaseColliderSize = true;
		}

		box.size = baseColliderSize * multiplier;
	}

	/// <summary>
	/// 機体の性能を取得する。未設定なら既定値（すべて等倍）を返す
	/// </summary>
	/// <param name="index">機体スロット番号</param>
	public PlaneSpec GetPlaneSpec(int index)
	{
		if (planeSpecs != null && 0 <= index && index < planeSpecs.Length && planeSpecs[index] != null)
		{
			return planeSpecs[index];
		}
		return new PlaneSpec();
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
			isBoosting = true;
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
			isBoosting = false;
			paticlePrefab.SetActive(false);
			ChangeForwordMoveSpeed(changeForwordMoveSpeedPerSecond * -0.5f * Time.deltaTime);
			ChangeVerticalAndHorizontalMoveSpeed(-changeVerticalAndHorizontalMoveSpeedPerSecond * Time.deltaTime);
		}
	}

	void FixedUpdate()
	{
		// ゴール後・衝突後はキネマティックにして固定してある。
		// ここで velocity を触ると「キネマティックな剛体に速度は設定できない」警告が毎フレーム出る
		if (hasGoaled == true || hasCrashed == true)
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
		if (maxFuel <= currentFuel)
		{
			currentFuel = maxFuel;
		}
	}

	void OnTriggerEnter(Collider collider)
	{
		if (collider.CompareTag("Coin") == true)
		{
			gameManager.AddCoin(collider.GetComponent<Coin>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Light);
			SpawnPickupEffect(coinPickupEffect, coinPickupEffectScale);
		}

		if (collider.CompareTag("Fuel") == true)
		{
			AddFuel(collider.GetComponent<Fuel>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Medium);
			SpawnPickupEffect(fuelPickupEffect, fuelPickupEffectScale);
		}

		if (collider.CompareTag("Timer") == true)
		{
			gameManager.AddTimer(collider.GetComponent<Timer>().Value);
			HapticFeedback.Play(HapticFeedback.Strength.Medium);
			SpawnPickupEffect(timerPickupEffect, timerPickupEffectScale);
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

			// ブーストの炎と軌跡はここで消す。IsPlay を false にすると Update が Accelerate() まで
			// 到達しなくなるので、消灯処理が走らないまま出っぱなしになってしまう
			isBoosting = false;
			paticlePrefab.SetActive(false);

			// 操作と制限時間を止める。止めないとリザルトを見ている間に時間切れになる
			gameManager.IsPlay = false;
			FreezePlane();

			// ステージが切り替わる前にコインを保存する
			gameManager.SaveCoin();

			// シーンの切り替えはリザルトの「NEXT」を押してから。
			// ゴールした瞬間に切り替えると、次ステージは先読み済みなので次のフレームで
			// シーンが変わってしまい、ゴール文字が映らないまま消えていた
			ui.ShowResult(gameManager.PlayTime, stageCoin, bonusCoin, GoToNextStage);
		}
	}

	/// <summary>
	/// アイテムを取ったことを知らせるエフェクトを、機体の定位置に出す。
	///
	/// 機体の子にして追従させる。
	/// ワールド座標に置き去りにすると、機体は毎秒300で前進するので0.25秒後には
	/// カメラの72ユニット後方まで流れてしまい、実測では1〜2フレームしか映らなかった。
	///
	/// 位置は「拾ったアイテムの座標」ではなく機体基準の定位置にする。
	/// アイテム座標のまま子にすると、拾った瞬間の機体とアイテムのずれを保ったまま
	/// 追従するので、機体の横に浮いたエフェクトが並走することになる
	/// </summary>
	/// <param name="prefab">出すエフェクト。未設定なら何もしない</param>
	/// <param name="scale">エフェクトの大きさ</param>
	void SpawnPickupEffect(GameObject prefab, float scale)
	{
		if (prefab == null)
		{
			return;
		}

		GameObject effect = Instantiate(prefab, this.transform);
		effect.transform.localPosition = pickupEffectOffset;
		effect.transform.localRotation = Quaternion.identity;
		effect.transform.localScale = Vector3.one * scale;
		Destroy(effect, Pickup_Effect_Lifetime_Seconds);
	}

	/// <summary>
	/// 機体を完全に止める。
	/// 速度を0にするだけでは足りない。ゴール枠や建物に接触した状態だと、
	/// 物理側のめり込み解消で機体が押し出され続け、カメラの外まで飛んでいってしまう。
	/// キネマティックにすると押し出しも回転も起きなくなる
	/// </summary>
	void FreezePlane()
	{
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.isKinematic = true;
	}

	/// <summary>
	/// 機体の見た目を消す。爆発したのに機体がそのまま浮いていると違和感が出る
	/// </summary>
	void HidePlaneModel()
	{
		for (int i = 0; i < planePrefabs.Length; i++)
		{
			if (planePrefabs[i] != null)
			{
				planePrefabs[i].SetActive(false);
			}
		}
	}

	/// <summary>
	/// 指定した位置に爆発を出す
	/// </summary>
	/// <param name="position">爆発を出す位置</param>
	void SpawnExplosion(Vector3 position)
	{
		if (explosionPrefab == null)
		{
			return;
		}

		GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
		explosion.transform.localScale = Vector3.one * explosionScale;
		Destroy(explosion, Explosion_Lifetime_Seconds);

		SpawnShockwave(position);
	}

	/// <summary>
	/// 衝突地点に衝撃波のリングを出す。
	///
	/// リングはカメラの方を向かせる。板ポリなので、真横から見ると線にしか見えない。
	/// 衝突地点はカメラのすぐ前なので、向きを合わせないと衝撃波として認識できない
	/// </summary>
	/// <param name="position">出す位置</param>
	void SpawnShockwave(Vector3 position)
	{
		if (shockwavePrefab == null)
		{
			return;
		}

		Quaternion rotation = Quaternion.identity;
		Camera camera = Camera.main;
		if (camera != null)
		{
			rotation = Quaternion.LookRotation(position - camera.transform.position);
		}

		GameObject shockwave = Instantiate(shockwavePrefab, position, rotation);
		shockwave.transform.localScale = Vector3.one * shockwaveScale;
		Destroy(shockwave, Explosion_Lifetime_Seconds);
	}

	/// <summary>
	/// 機体を接触した地点まで戻す。
	///
	/// 機体は毎秒300ユニット進むので、衝突を検知した時点では1物理ステップぶん
	/// （約6ユニット）建物にめり込んだ先まで進んでいる。そのまま止めると、
	/// 機体の2.5ユニット後ろにいるカメラが接触点を追い越してしまい、
	/// 爆発が建物の中や画面の外に出てしまう。
	/// 接触点まで戻してから止めることで、機体の位置に出す爆発がカメラの正面に来る
	/// </summary>
	/// <param name="collision">衝突情報</param>
	void MoveBackToImpactPoint(Collision collision)
	{
		if (collision.contactCount <= 0)
		{
			return;
		}

		Vector3 impactPoint = collision.GetContact(0).point;
		this.transform.position = impactPoint;
		rb.position = impactPoint;
	}

	/// <summary>
	/// 爆発音を鳴らす。
	/// 1秒後にステージが切り替わると、このシーンにある音源は破棄されて音が途中で切れる。
	/// シーンに属さない一時オブジェクトから鳴らすことで、暗転をまたいで最後まで聞こえるようにする
	/// </summary>
	void PlayExplosionSound()
	{
		if (explosionSound == null)
		{
			return;
		}

		GameObject soundObject = new GameObject("ExplosionSound");
		DontDestroyOnLoad(soundObject);

		AudioSource source = soundObject.AddComponent<AudioSource>();
		source.clip = explosionSound;
		source.volume = explosionVolume;
		// 衝突地点はカメラのすぐ前なので、距離減衰を掛けずに2Dで鳴らす
		source.spatialBlend = 0f;
		source.Play();

		Destroy(soundObject, explosionSound.length + 0.1f);
	}

	/// <summary>
	/// 爆発を見せてから次のステージへ切り替える。
	/// 衝突と同時に切り替えると、暗転が入って爆発が1フレームも見えない
	/// </summary>
	IEnumerator SwitchStageAfterExplosion()
	{
		yield return new WaitForSeconds(explosionViewSeconds);

		if (AdsManager.SingletonInstance != null)
		{
			AdsManager.SingletonInstance.ShowAdsInterstitialCount();
		}

		ui.FadeIn();
		StageManager.SingletonInstance.IsTriggered = true;
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
			// 複数の面に同時に当たっても、爆発とステージ切り替えを二重に走らせない
			if (hasCrashed == true)
			{
				return;
			}

			Debug.Log("障害物に衝突した");

			// めり込んだ先まで進んでいるので、接触点まで機体を戻してから爆発を出す
			MoveBackToImpactPoint(collision);
			CrashAndAdvanceStage();
		}
	}

	/// <summary>
	/// 撃墜・衝突でステージを終わらせる。
	/// 爆発を見せてから次のステージへ切り替える。
	/// 障害物への衝突と対空砲の被弾で、同じ演出と流れを使う。
	///
	/// 爆発は必ず機体の位置に出す。接触点や弾の当たり判定の位置を渡していたときは、
	/// 判定の取り方しだいで機体から離れた場所に爆発が出てしまっていた
	/// （対空砲の弾は半径4ユニットの球で当たりを取るので、その半径ぶんずれる）
	/// </summary>
	public void CrashAndAdvanceStage()
	{
		if (hasCrashed == true || hasGoaled == true)
		{
			return;
		}
		hasCrashed = true;

		HapticFeedback.Play(HapticFeedback.Strength.VeryHeavy);

		// 機体の位置に出す。衝突なら直前に接触点まで戻してあるので、機体も爆発も接触点に来る
		SpawnExplosion(this.transform.position);
		PlayExplosionSound();

		// ブーストの炎と軌跡を消し、機体を止めて見た目も消す。
		// 衝突後は Update が Accelerate() まで到達しないので、ここで下ろさないと
		// 撃墜されたのに軌跡が伸び続ける
		isBoosting = false;
		paticlePrefab.SetActive(false);
		FreezePlane();
		HidePlaneModel();

		// 爆発を見せている間に時間切れにならないよう止める。
		// IsPlay が false になると UI が「TAP!」を出そうとするので、先に知らせておく
		ui.ShowCrash();
		gameManager.IsPlay = false;
		// ステージが切り替わる前にコインを保存する
		gameManager.SaveCoin();

		// 暗転とシーン切り替えは爆発を見せてから
		StartCoroutine(SwitchStageAfterExplosion());
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