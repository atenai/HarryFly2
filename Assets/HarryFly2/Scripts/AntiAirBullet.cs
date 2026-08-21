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

	[Tooltip("当たり判定の半径。機体のコライダーは1.5ユニット角と小さいので、点の判定ではまず当たらない")]
	[SerializeField] float hitRadius = 4f;

	[Tooltip("消えるまでの時間（秒）。当たらなかった弾を残さない")]
	[SerializeField] float lifetimeSeconds = 5f;

	/// <summary>撃たれてからの経過時間</summary>
	float elapsed = 0f;

	/// <summary>二重に当てないための判定</summary>
	bool hasHit = false;

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

			hasHit = true;
			Debug.Log("対空砲の弾に被弾した");
			// 被弾したらその場でステージ終了。障害物への衝突と同じ流れに乗せる。
			// 爆発の位置は機体側で機体の位置に合わせる。
			// ここで hits[i].point を渡すと、半径4ユニットの球で取った当たり判定の点が
			// そのまま爆発の位置になり、機体から離れた場所で爆発してしまう
			plane.CrashAndAdvanceStage();
			Destroy(this.gameObject);
			return;
		}

		this.transform.position = origin + direction * step;
	}
}
