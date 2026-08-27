using UnityEngine;

/// <summary>
/// 対空機関砲が撃つ弾。
///
/// 機体は毎秒300ユニットで飛び、弾も高速なので、トリガーの重なり判定では
/// 1フレームの移動量が大きすぎてすり抜ける。
/// そのため、毎フレームの移動ぶんを Raycast で掃いて当たりを取る
/// </summary>
public class AntiAirBullet : MonoBehaviour
{
	[Tooltip("弾速（1秒あたりの移動量）。機体の前進速度300より速くしないと追いつけない")]
	[SerializeField] float speed = 420f;
	public float Speed => speed;

	/// <summary>
	/// 当たり判定の半径。
	///
	/// 移動ぶんを掃いて調べているので、すり抜け対策として大きくする必要はない。
	/// 大きくすると「曳光弾が明らかに横を通ったのに撃墜された」ことになるので、
	/// 弾の見た目（幅1.2ユニット）から離れすぎない範囲に収める。
	/// 命中率は AntiAirGun 側の aimSpreadDegrees と leadRatio で調整すること
	/// </summary>
	[Tooltip("当たり判定の半径。大きくすると弾が外れて見えるのに撃墜されるようになる")]
	[SerializeField] float hitRadius = 2.5f;

	[Tooltip("消えるまでの時間（秒）。当たらなかった弾を残さない")]
	[SerializeField] float lifetimeSeconds = 5f;

	/// <summary>
	/// この距離まで近づいたら「かすめた」音を鳴らす。
	/// 当たり判定の半径より広くしないと、外れた弾では一度も鳴らない
	/// </summary>
	[Tooltip("かすめた音を鳴らす距離。当たり判定の半径より広くする")]
	[SerializeField] float nearMissDistance = 14f;

	/// <summary>撃たれてからの経過時間</summary>
	float elapsed = 0f;

	/// <summary>二重に当てないための判定</summary>
	bool hasHit = false;

	/// <summary>かすめた音を鳴らしたかどうか。1発につき1回だけ鳴らす</summary>
	bool hasPlayedNearMiss = false;

	/// <summary>音を鳴らしてもらう相手。毎フレーム探し直さないように覚えておく</summary>
	PlaneController cachedPlane;

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

	void Update()
	{
		if (hasHit == true)
		{
			return;
		}

		elapsed = elapsed + Time.deltaTime;
		if (lifetimeSeconds <= elapsed)
		{
			Destroy(this.gameObject);
			return;
		}

		float step = speed * Time.deltaTime;
		Vector3 origin = this.transform.position;
		Vector3 direction = this.transform.forward;

		// 移動ぶんを掃いて、間に機体が入っていないか調べる。
		// アイテムのトリガーに反応しないよう除外する
		RaycastHit[] hits = Physics.SphereCastAll(origin, hitRadius, direction, step, ~0, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < hits.Length; i++)
		{
			PlaneController plane = hits[i].collider.GetComponentInParent<PlaneController>();
			if (plane == null)
			{
				continue;
			}

			// 次ステージは飛行中に裏で先読みしており、読み込み直後は同じ座標に
			// もう1機の機体が居る。撃った側と同じステージの機体だけを狙う
			if (plane.gameObject.scene != this.gameObject.scene)
			{
				continue;
			}

			hasHit = true;
			Debug.Log("対空砲の弾に被弾した");
			// 被弾したらその場でステージ終了。障害物への衝突と同じ流れに乗せる。
			// 爆発の位置は機体側で機体の位置に合わせる。
			// ここで hits[i].point を渡すと、半径4ユニットの球で取った当たり判定の点が
			// そのまま爆発の位置になり、機体から離れた場所で爆発してしまう
			plane.CrashAndAdvanceStage(PlaneController.CrashCause.ShotDown);
			Destroy(this.gameObject);
			return;
		}

		this.transform.position = origin + direction * step;

		CheckNearMiss();
	}

	/// <summary>
	/// 機体の近くを通り抜けたら音を鳴らす。
	///
	/// 鳴らすのは機体側に任せる。この弾は当たれば即 Destroy されるし寿命でも消えるので、
	/// 自分で AudioSource を持つと鳴っている途中で切れてしまう
	/// </summary>
	void CheckNearMiss()
	{
		if (hasPlayedNearMiss == true || nearMissDistance <= 0f)
		{
			return;
		}

		if (cachedPlane == null)
		{
			cachedPlane = Object.FindObjectOfType<PlaneController>();
			if (cachedPlane == null)
			{
				return;
			}
		}

		// 先読み中の次ステージにも機体が居るので、同じステージのものだけ相手にする
		if (cachedPlane.gameObject.scene != this.gameObject.scene)
		{
			return;
		}

		float distance = Vector3.Distance(this.transform.position, cachedPlane.transform.position);
		if (distance <= nearMissDistance)
		{
			hasPlayedNearMiss = true;
			cachedPlane.PlayNearMissSound();
		}
	}
}
