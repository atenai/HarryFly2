using UnityEngine;

/// <summary>
/// 障害物を上下・左右・斜めに往復させる。
///
/// 静的コライダーを transform で直接動かすと PhysX が当たり判定の木構造を毎回作り直すため重い。
/// キネマティックな Rigidbody を付けて MovePosition で動かすことで、
/// 衝突判定（PlaneController の OnCollisionEnter）も正しく発生する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ObstacleMover : MonoBehaviour
{
	/// <summary>動く向き</summary>
	public enum MoveDirection
	{
		/// <summary>上下</summary>
		Vertical,
		/// <summary>左右</summary>
		Horizontal,
		/// <summary>斜め（右上と左下を往復）</summary>
		DiagonalRight,
		/// <summary>斜め（左上と右下を往復）</summary>
		DiagonalLeft,
	}

	[Tooltip("動く向き")]
	[SerializeField] MoveDirection direction = MoveDirection.Vertical;

	[Tooltip("開始位置からの片振幅。実際は この2倍 の幅を往復する")]
	[SerializeField] float amplitude = 15f;

	[Tooltip("移動速度（1秒あたりの移動量）")]
	[SerializeField] float speed = 10f;

	[Tooltip("開始位相をずらす秒数。全部の障害物が同じ動きに揃わないようにする")]
	[SerializeField] float phaseOffsetSeconds = 0f;

	/// <summary>配置されたときの位置。ここを中心に往復する</summary>
	Vector3 startPosition;
	Rigidbody cachedRigidbody;

	void Awake()
	{
		startPosition = transform.position;

		cachedRigidbody = GetComponent<Rigidbody>();
		// 落下させず、こちらから位置を与えるだけにする
		cachedRigidbody.isKinematic = true;
		cachedRigidbody.useGravity = false;
		// 物理ステップ間の見た目を滑らかにする
		cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		// キネマティックな剛体は Continuous / Continuous Dynamic の対象外なので、
		// Discrete のままだと毎秒300〜1500で飛んでくる機体との判定を取りこぼす。
		// キネマティックでも効く Continuous Speculative にしておく
		cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
	}

	void FixedUpdate()
	{
		cachedRigidbody.MovePosition(startPosition + GetAxis() * GetTravel());
	}

	/// <summary>
	/// 中心からのずれ。Time.time の関数なので、フレームレートが変わっても軌道は変わらない
	/// </summary>
	float GetTravel()
	{
		if (amplitude <= 0f)
		{
			return 0f;
		}
		return Mathf.PingPong((Time.time + phaseOffsetSeconds) * speed, amplitude * 2f) - amplitude;
	}

	Vector3 GetAxis()
	{
		switch (direction)
		{
			case MoveDirection.Horizontal:
				return Vector3.right;
			case MoveDirection.DiagonalRight:
				return new Vector3(1f, 1f, 0f).normalized;
			case MoveDirection.DiagonalLeft:
				return new Vector3(-1f, 1f, 0f).normalized;
			default:
				return Vector3.up;
		}
	}

	/// <summary>
	/// 配置時にまとめて設定するための入口
	/// </summary>
	public void Setup(MoveDirection newDirection, float newAmplitude, float newSpeed, float newPhaseOffsetSeconds)
	{
		direction = newDirection;
		amplitude = newAmplitude;
		speed = newSpeed;
		phaseOffsetSeconds = newPhaseOffsetSeconds;
	}
}
