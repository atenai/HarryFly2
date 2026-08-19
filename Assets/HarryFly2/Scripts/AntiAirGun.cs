using UnityEngine;

/// <summary>
/// 対空機関砲。近づいてきた機体を狙って弾を撃つ。
/// 弾に当たるとその場でステージ終了になる（処理は AntiAirBullet 側）
/// </summary>
public class AntiAirGun : MonoBehaviour
{
	[Tooltip("狙って回転させる砲身。未設定ならこのオブジェクト自身を回す")]
	[SerializeField] Transform barrel;

	[Tooltip("弾を出す位置。未設定なら砲身の位置から出す")]
	[SerializeField] Transform muzzle;

	[Tooltip("弾のプレハブ")]
	[SerializeField] GameObject bulletPrefab;

	[Tooltip("発砲時に砲口へ出す発光エフェクト")]
	[SerializeField] GameObject muzzleFlashPrefab;

	[Tooltip("発光エフェクトの大きさ。砲の大きさに合わせて調整する")]
	[SerializeField] float muzzleFlashScale = 1f;

	[Tooltip("この距離まで近づいたら撃ち始める")]
	[SerializeField] float fireRange = 700f;

	[Tooltip("撃つ間隔（秒）")]
	[SerializeField] float fireIntervalSeconds = 1.1f;

	[Tooltip("砲身が機体を追う速さ（1秒あたりの角度）")]
	[SerializeField] float aimDegreesPerSecond = 90f;

	[Tooltip("狙いのばらつき（度）。0だと必中で避けられない。大きすぎると当たらない")]
	[SerializeField] float aimSpreadDegrees = 1.2f;

	[Tooltip("機体の進む先を読んで撃つ割合。1で完全に読む、0で現在位置を狙う")]
	[SerializeField, Range(0f, 1f)] float leadRatio = 0.9f;

	/// <summary>次に撃てるようになる時刻</summary>
	float nextFireTime = 0f;

	/// <summary>狙う相手。ステージごとに1機なので最初に見つけて覚えておく</summary>
	PlaneController target;

	/// <summary>狙う相手の剛体。先読みと通過判定で毎フレーム使うので、相手と一緒に覚えておく</summary>
	Rigidbody targetBody;

	/// <summary>ゲームが始まる前や終わった後は撃たない</summary>
	GameManager gameManager;

	void Start()
	{
		if (barrel == null)
		{
			barrel = this.transform;
		}
		// 最初の1発をいきなり撃たないよう、間隔ぶんずらしておく
		nextFireTime = Time.time + fireIntervalSeconds;
	}

	void Update()
	{
		if (target == null)
		{
			target = Object.FindObjectOfType<PlaneController>();
			if (target == null)
			{
				return;
			}
			targetBody = target.GetComponent<Rigidbody>();
		}

		if (gameManager == null)
		{
			gameManager = Object.FindObjectOfType<GameManager>();
		}

		// プレイ中以外は動かさない。ゴール後やリザルト中に撃たれると理不尽になる
		if (gameManager == null || gameManager.IsPlay == false)
		{
			return;
		}

		Vector3 toTarget = target.transform.position - barrel.position;
		float distance = toTarget.magnitude;
		if (fireRange < distance)
		{
			return;
		}

		// 砲身を機体の方へ徐々に向ける
		Quaternion wanted = Quaternion.LookRotation(toTarget.normalized);
		barrel.rotation = Quaternion.RotateTowards(barrel.rotation, wanted, aimDegreesPerSecond * Time.deltaTime);

		// 追い抜かれたら撃つのをやめる。砲身は向き続けるので見た目は追尾したまま。
		// 弾速420に対して機体は300なので背後からでも追いつくが、カメラは常に進行方向を
		// 向いているため、その弾は一度も画面に映らない。
		// 何に撃たれたのか分からないまま撃墜されることになるので撃たせない
		if (HasPassed(toTarget) == true)
		{
			return;
		}

		if (Time.time < nextFireTime)
		{
			return;
		}
		nextFireTime = Time.time + fireIntervalSeconds;

		// 狙いは実際に弾が出る位置を基準に計算する。
		// 砲身の根元を基準にすると、旋回中は砲口が20ユニット横にずれているぶん
		// そのまま平行なズレになって当たらない
		Vector3 origin = muzzle != null ? muzzle.position : barrel.position;
		Fire(origin, GetAimDirection(origin));
	}

	/// <summary>
	/// 撃つ向きを決める。
	/// 機体は毎秒300ユニットで前進し続けるので、現在位置を狙うと必ず後ろへ抜ける。
	/// 弾が届くまでに機体が進む距離を見込んで、その先を狙う
	/// </summary>
	/// <param name="origin">実際に弾が出る位置</param>
	Vector3 GetAimDirection(Vector3 origin)
	{
		Vector3 targetPosition = target.transform.position;
		Vector3 aimPoint = targetPosition;

		float bulletSpeed = GetBulletSpeed();
		if (targetBody != null && bulletSpeed > 0f)
		{
			// 1回の計算では足りない。先読みした地点は今の位置より遠いので飛行時間が伸び、
			// その間に機体はさらに前進する。狙点と飛行時間を数回すり合わせて収束させる
			for (int i = 0; i < Lead_Refine_Count; i++)
			{
				float travelTime = Vector3.Distance(aimPoint, origin) / bulletSpeed;
				aimPoint = targetPosition + targetBody.velocity * travelTime * leadRatio;
			}
		}

		return (aimPoint - origin).normalized;
	}

	/// <summary>先読み地点を求める反復回数。3回でほぼ収束する</summary>
	const int Lead_Refine_Count = 4;

	/// <summary>
	/// 機体が砲を追い抜いたかどうか。
	/// 砲から機体へのベクトルが機体の進行方向と同じ向きになったら、追い抜かれている
	/// </summary>
	/// <param name="toTarget">砲から機体へのベクトル</param>
	bool HasPassed(Vector3 toTarget)
	{
		// 停止中（ゴール後など）は進行方向が取れないので、通り過ぎたとは見なさない
		Vector3 travelDirection = targetBody != null ? targetBody.velocity : Vector3.zero;
		if (travelDirection.sqrMagnitude <= 0.0001f)
		{
			return false;
		}

		return 0f < Vector3.Dot(toTarget, travelDirection);
	}

	/// <summary>
	/// 弾速を取得する。先読みの計算に使う
	/// </summary>
	float GetBulletSpeed()
	{
		if (bulletPrefab == null)
		{
			return 0f;
		}
		AntiAirBullet bullet = bulletPrefab.GetComponent<AntiAirBullet>();
		return bullet != null ? bullet.Speed : 0f;
	}

	/// <summary>
	/// 弾を撃つ
	/// </summary>
	/// <param name="direction">機体への向き</param>
	void Fire(Vector3 origin, Vector3 direction)
	{
		if (bulletPrefab == null)
		{
			return;
		}

		// 必中だと避けようがないので、狙いを少しばらけさせる
		Vector3 spread = Random.insideUnitSphere * Mathf.Tan(aimSpreadDegrees * Mathf.Deg2Rad);
		Vector3 aim = (direction + spread).normalized;

		GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(aim));

		AntiAirBullet bulletScript = bullet.GetComponent<AntiAirBullet>();
		if (bulletScript != null)
		{
			bulletScript.Launch(aim);
		}

		SpawnMuzzleFlash(origin, aim);
	}

	/// <summary>
	/// 砲口を光らせる。
	/// 撃たれたことに気づく手がかりが曳光弾しかないと、
	/// 遠くから撃たれている間はまず気づけず、当たって初めて砲の存在を知ることになる
	/// </summary>
	/// <param name="origin">弾が出る位置</param>
	/// <param name="direction">撃つ向き</param>
	void SpawnMuzzleFlash(Vector3 origin, Vector3 direction)
	{
		if (muzzleFlashPrefab == null)
		{
			return;
		}

		// 砲身は機体を追って回り続けるので、子にして追従させる。
		// 再生し終わればプレハブ側の設定（Stop Action = Destroy）で自分から消えるため、
		// ここで寿命を管理する必要はない
		GameObject flash = Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(direction), barrel);
		flash.transform.localScale = Vector3.one * muzzleFlashScale;
	}
}
