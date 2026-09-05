using UnityEngine;

/// <summary>
/// 対空機関砲が撃つ弾。
///
/// 当たりは Physics を使わず、この物理ステップで弾と機体が最も近づいた距離で取る。
/// 砲は機体の進む向きと交差する向きへ撃つので、弾と機体は互いにすれ違う形になる。
/// 弾だけを掃いて調べていると、機体側の移動ぶん（ブースト中は1ステップに30ユニット）が
/// 抜け落ちて、弾の間をすり抜けてしまう。
/// 両方の移動を同じ時刻で見比べれば、どれだけ速くても取りこぼさない。
///
/// 「同じ時刻」を守れるのは物理の刻みの上だけなので、この弾は FixedUpdate で動かす。
/// 機体は剛体で、動くのは物理ステップのときだけだからである
/// </summary>
///
/// <remarks>
/// 実行順を機体より後ろにしている。
/// 機体は FixedUpdate で速度を入れ直し、壁際では位置も補正するので、
/// 先に走ると1ステップ古い値で当たりを取ることになる
/// </remarks>
[DefaultExecutionOrder(100)]
public class AntiAirBullet : MonoBehaviour
{
	[Tooltip("弾速（1秒あたりの移動量）。速すぎると曳光弾が視界を通り過ぎるだけになる")]
	[SerializeField] float speed = 300f;

	/// <summary>
	/// 当たり判定の半径。
	///
	/// 機体側の大きさはここに足されるので、見た目の弾の太さに合わせておく。
	/// 大きくすると「曳光弾が明らかに横を通ったのに撃墜された」ことになる
	/// </summary>
	[Tooltip("当たり判定の半径。機体の大きさは別に足されるので、弾の見た目の太さに合わせる")]
	[SerializeField] float hitRadius = 1.6f;

	[Tooltip("消えるまでの時間（秒）。当たらなかった弾を残さない")]
	[SerializeField] float lifetimeSeconds = 5f;

	/// <summary>
	/// この距離まで近づいたら「かすめた」音を鳴らす。
	/// 当たり判定の半径より広くしないと、外れた弾では一度も鳴らない
	/// </summary>
	[Tooltip("かすめた音を鳴らす距離。当たり判定の半径より広くする")]
	[SerializeField] float nearMissDistance = 14f;

	/// <summary>
	/// 被弾した瞬間に機体の位置へ出す火花。
	///
	/// 機体は被弾すると爆発して墜ちるが、爆発だけだと障害物への激突と見分けが付かない。
	/// 弾が刺さった火花が出れば、撃たれて墜ちたことがその場で分かる
	/// </summary>
	[Tooltip("被弾した瞬間に機体の位置へ出す火花。未設定なら出さない")]
	[SerializeField] GameObject hitEffectPrefab;

	[Tooltip("被弾の火花の大きさ")]
	[SerializeField] float hitEffectScale = 1.5f;

	/// <summary>被弾の火花を消すまでの時間（秒）。エフェクト側で消え損ねたときの保険</summary>
	const float Hit_Effect_Lifetime_Seconds = 3f;

	/// <summary>撃たれてからの経過時間</summary>
	float elapsed = 0f;

	/// <summary>二重に当てないための判定</summary>
	bool hasHit = false;

	/// <summary>かすめた音を鳴らしたかどうか。1発につき1回だけ鳴らす</summary>
	bool hasPlayedNearMiss = false;

	/// <summary>狙う相手。毎フレーム探し直さないように覚えておく</summary>
	PlaneController cachedPlane;

	/// <summary>相手の剛体。1フレームでどれだけ動いたかを知るために使う</summary>
	Rigidbody cachedPlaneBody;

	/// <summary>相手の当たり判定。機体ごとに大きさが変わるので、毎回そこから読む</summary>
	BoxCollider cachedPlaneCollider;

	/// <summary>
	/// 発射方向を決める
	/// </summary>
	/// <param name="direction">飛んでいく向き</param>
	public void Launch(Vector3 direction)
	{
		if (direction.sqrMagnitude <= 0.0001f)
		{
			return;
		}
		this.transform.rotation = Quaternion.LookRotation(direction.normalized);
	}

	/// <summary>止まっているかどうか。通信が切れたら飛ばさない</summary>
	bool hasStopped = false;

	/// <summary>
	/// 飛んでいる弾をその場で止める。時間切れで通信が切れた演出から呼ばれる。
	///
	/// 消さずに残すのは、他のものが止まっている中で弾だけ消えると
	/// 何が起きたのか分からなくなるため
	/// </summary>
	public void StopMoving()
	{
		hasStopped = true;
	}

	void FixedUpdate()
	{
		if (hasHit == true || hasStopped == true)
		{
			return;
		}

		// 機体は剛体なので、動くのは物理の刻み（0.02秒）だけ。
		// 弾を描画の刻み（60fpsなら0.0167秒）で進めると、
		// 弾と機体が別々の時計で動くことになり、
		// 「同じ時刻で見比べる」というこの判定の前提そのものが崩れる
		float step = Time.fixedDeltaTime;

		elapsed = elapsed + step;
		if (lifetimeSeconds <= elapsed)
		{
			Destroy(this.gameObject);
			return;
		}

		Vector3 from = this.transform.position;
		Vector3 to = from + this.transform.forward * (speed * step);

		float distance = GetClosestDistanceToPlane(from, to);

		if (distance <= hitRadius + GetPlaneRadius())
		{
			HitPlane();
			return;
		}

		this.transform.position = to;

		CheckNearMiss(distance);
	}

	/// <summary>
	/// この物理ステップの間に弾と機体が最も近づいた距離を返す。
	///
	/// 弾は from から to へ、機体も同じ時間で動いている。
	/// 両者の差を1本のベクトルとして見れば、最も近づく瞬間は
	/// その差が最短になる位置として一度に求められる。
	/// 弾と機体を別々の線分として見比べてはいけない。
	/// それでは「弾が通った場所を、機体が別の時刻に通った」だけでも当たったことになってしまう
	/// </summary>
	/// <param name="from">このステップの初めの弾の位置</param>
	/// <param name="to">このステップの終わりの弾の位置</param>
	float GetClosestDistanceToPlane(Vector3 from, Vector3 to)
	{
		if (FindPlane() == false)
		{
			return float.MaxValue;
		}

		// 機体がこのステップで動くぶん。
		// 剛体には重力も抗力も掛かっておらず、機体側が毎ステップ velocity を直接入れているので、
		// このあとの物理演算で機体はちょうどこのぶんだけ進む
		Vector3 planeMove = cachedPlaneBody != null
			? cachedPlaneBody.velocity * Time.fixedDeltaTime
			: Vector3.zero;

		Vector3 planeFrom = GetPlaneCenter();

		Vector3 gap = from - planeFrom;
		Vector3 gapChange = (to - from) - planeMove;

		float rate = 0f;
		float gapChangeSqr = gapChange.sqrMagnitude;
		if (Mathf.Epsilon < gapChangeSqr)
		{
			// 差が最短になる時刻。フレームの外まで伸ばさないよう0〜1に収める
			rate = Mathf.Clamp01(-Vector3.Dot(gap, gapChange) / gapChangeSqr);
		}

		return (gap + gapChange * rate).magnitude;
	}

	/// <summary>
	/// このステップの初めの、機体の当たり判定の中心。
	///
	/// 剛体の位置を使う。transform.position から機体の移動ぶんを引いて求めてはいけない。
	/// 補間を切ってあるので transform は最後の物理ステップの位置で止まっており、
	/// そこからさらに1描画フレームぶん引くと、実際より最大で1ステップ＋1フレーム後ろを指す。
	/// ブースト中はそれが55ユニットにもなり、判定半径2.6の20倍ずれる。
	/// 当たり判定が機体の後ろに張り付いて、機体を貫く弾が当たらなくなっていた。
	///
	/// 当たり判定の箱は機体の原点から少しずれた位置にあるので、その分も足す
	/// </summary>
	Vector3 GetPlaneCenter()
	{
		Vector3 origin = cachedPlaneBody != null
			? cachedPlaneBody.position
			: cachedPlane.transform.position;

		if (cachedPlaneCollider == null)
		{
			return origin;
		}

		// 箱の中心は機体のローカル座標なので、機体の向きと大きさを通してから足す
		return origin + cachedPlane.transform.TransformVector(cachedPlaneCollider.center);
	}

	/// <summary>
	/// 機体側の当たり判定の大きさ。
	/// 機体はショップで選んだ機種によって当たり判定の大きさが変わるので、その場で読む
	/// </summary>
	float GetPlaneRadius()
	{
		if (cachedPlaneCollider == null)
		{
			return 0f;
		}

		Vector3 halfSize = Vector3.Scale(cachedPlaneCollider.size, cachedPlane.transform.lossyScale) * 0.5f;
		return halfSize.magnitude;
	}

	/// <summary>
	/// 狙う相手を探す。
	///
	/// 次ステージは飛行中に裏で先読みしており、読み込み直後は同じ座標にもう1機の機体が居る。
	/// 撃った側と同じステージの機体だけを相手にしないと、
	/// 隣のステージの機体を撃って自分のステージでは何も起きない、ということになる
	/// </summary>
	/// <returns>相手が見つかったかどうか</returns>
	bool FindPlane()
	{
		if (cachedPlane != null)
		{
			return true;
		}

		PlaneController[] planes = Object.FindObjectsOfType<PlaneController>();
		for (int i = 0; i < planes.Length; i++)
		{
			if (planes[i].gameObject.scene != this.gameObject.scene)
			{
				continue;
			}

			cachedPlane = planes[i];
			cachedPlaneBody = planes[i].GetComponent<Rigidbody>();
			cachedPlaneCollider = planes[i].GetComponent<BoxCollider>();
			return true;
		}

		return false;
	}

	/// <summary>
	/// 被弾させる。
	/// その場でステージ終了。障害物への衝突と同じ流れに乗せる
	/// </summary>
	void HitPlane()
	{
		hasHit = true;
		Debug.Log("対空砲の弾に被弾した");

		SpawnHitEffect(cachedPlane.transform.position);

		// 爆発の位置は機体側で機体の位置に合わせる。
		// ここで当たった点を渡すと、判定に使った半径のぶんだけ機体から離れた場所で爆発してしまう
		cachedPlane.CrashAndAdvanceStage(PlaneController.CrashCause.ShotDown);
		Destroy(this.gameObject);
	}

	/// <summary>
	/// 被弾の火花を出す。
	///
	/// 弾の子にはしない。この弾は直後に Destroy されるので、一緒に消えてしまう
	/// </summary>
	/// <param name="position">出す位置</param>
	void SpawnHitEffect(Vector3 position)
	{
		if (hitEffectPrefab == null)
		{
			return;
		}

		GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(-this.transform.forward));
		effect.transform.localScale = Vector3.one * hitEffectScale;
		Destroy(effect, Hit_Effect_Lifetime_Seconds);
	}

	/// <summary>
	/// 機体の近くを通り抜けたら音を鳴らす。
	///
	/// 鳴らすのは機体側に任せる。この弾は当たれば即 Destroy されるし寿命でも消えるので、
	/// 自分で AudioSource を持つと鳴っている途中で切れてしまう
	/// </summary>
	/// <param name="distance">このフレームで機体に最も近づいた距離</param>
	void CheckNearMiss(float distance)
	{
		if (hasPlayedNearMiss == true || nearMissDistance <= 0f || cachedPlane == null)
		{
			return;
		}

		if (nearMissDistance < distance)
		{
			return;
		}

		hasPlayedNearMiss = true;
		cachedPlane.PlayNearMissSound();
	}
}
