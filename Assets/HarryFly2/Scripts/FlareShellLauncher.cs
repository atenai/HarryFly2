using UnityEngine;

/// <summary>
/// 照明弾を定期的に打ち上げる。
///
/// 機体は毎秒300で前進し続けるので、決まった場所に置くとすぐ後ろへ流れてしまう。
/// 機体の前方に出し続けることで、飛んでいる間ずっと夜空に照明弾が見えるようにする。
/// </summary>
public class FlareShellLauncher : MonoBehaviour
{
	[Tooltip("打ち上げる照明弾のプレハブ")]
	[SerializeField] GameObject flareShellPrefab;

	[Tooltip("1回に打ち上げる発数")]
	[SerializeField] int shellsPerVolley = 3;

	[Tooltip("打ち上げの間隔（秒）")]
	[SerializeField] float volleyIntervalSeconds = 5f;

	[Tooltip("同時に上げると揃いすぎるので、1発ずつずらす時間（秒）")]
	[SerializeField] float shellStaggerSeconds = 0.6f;

	[Tooltip("機体のどれだけ前方に出すか")]
	[SerializeField] float aheadDistance = 700f;

	[Tooltip("前後方向のばらつき")]
	[SerializeField] float depthSpread = 300f;

	[Tooltip("左右方向のばらつき。機体の移動範囲（±50）より広く取って空全体に散らす")]
	[SerializeField] float horizontalSpread = 250f;

	[Tooltip("点火する高さの範囲（機体の高さを基準にした相対値）")]
	[SerializeField] float heightMin = -60f;
	[SerializeField] float heightMax = 10f;

	/// <summary>次に打ち上げる時刻</summary>
	float nextVolleyTime = 0f;

	/// <summary>この斉射で残っている発数</summary>
	int remainingInVolley = 0;

	/// <summary>次の1発を出す時刻</summary>
	float nextShellTime = 0f;

	/// <summary>基準にする機体。毎フレーム探し直さないように覚えておく</summary>
	PlaneController target;

	void Start()
	{
		// 開始と同時に上げず、少し間を置いてから始める
		nextVolleyTime = Time.time + volleyIntervalSeconds * 0.5f;
	}

	void Update()
	{
		if (flareShellPrefab == null)
		{
			return;
		}

		if (target == null)
		{
			target = Object.FindObjectOfType<PlaneController>();
			if (target == null)
			{
				return;
			}
		}

		if (remainingInVolley <= 0)
		{
			if (Time.time < nextVolleyTime)
			{
				return;
			}
			remainingInVolley = shellsPerVolley;
			nextShellTime = Time.time;
			nextVolleyTime = Time.time + volleyIntervalSeconds;
		}

		if (Time.time < nextShellTime)
		{
			return;
		}

		Launch();
		remainingInVolley = remainingInVolley - 1;
		nextShellTime = Time.time + shellStaggerSeconds;
	}

	/// <summary>
	/// 照明弾を1発、機体の前方に出す
	/// </summary>
	void Launch()
	{
		Vector3 basePosition = target.transform.position;

		Vector3 position = new Vector3(
			basePosition.x + Random.Range(-horizontalSpread, horizontalSpread),
			basePosition.y + Random.Range(heightMin, heightMax),
			basePosition.z + aheadDistance + Random.Range(-depthSpread, depthSpread));

		Instantiate(flareShellPrefab, position, Quaternion.identity);
	}
}
