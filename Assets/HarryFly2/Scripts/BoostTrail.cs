using UnityEngine;

/// <summary>
/// ブースト中だけ尾を引くトレイル。主翼端に付けてベイパートレイルにする。
///
/// 機体は通常でも毎秒300、ブースト中は最大1500で前進するので、
/// 軌跡そのものが速度計の役割を果たす。
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class BoostTrail : MonoBehaviour
{
	/// <summary>
	/// ブースト状態の取得元。
	/// このトレイルは機体の子に置く前提なので、親から辿って自動で見つける。
	/// インスペクタでの割り当て漏れを防ぐため、あえて SerializeField にしていない
	/// </summary>
	PlaneController plane;

	TrailRenderer trail;

	void Awake()
	{
		trail = GetComponent<TrailRenderer>();
		plane = GetComponentInParent<PlaneController>();

		// 出しっぱなしで始めると、ブーストしていないのに軌跡が伸びる
		trail.emitting = false;
		trail.Clear();
	}

	void LateUpdate()
	{
		if (plane == null)
		{
			return;
		}

		// 位置の更新後に判定する。Update で切り替えると、
		// 移動前の座標で点が打たれて軌跡が1フレームぶん遅れる
		trail.emitting = plane.IsBoosting;
	}
}
