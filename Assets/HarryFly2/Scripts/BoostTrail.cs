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
	/// ブーストをやめてから軌跡が消えるまでの時間（秒）。
	///
	/// これが無いと、点の寿命が尽きるのを待つだけになる。
	/// その場合は尾が短くなっていくものの濃さは最後まで変わらないため、
	/// 消える瞬間だけぶつ切りに見える
	/// </summary>
	[Tooltip("ブーストをやめてから軌跡が消えるまでの時間（秒）")]
	[SerializeField] float fadeOutSeconds = 0.7f;

	/// <summary>
	/// ブースト状態の取得元。
	/// このトレイルは機体の子に置く前提なので、親から辿って自動で見つける。
	/// インスペクタでの割り当て漏れを防ぐため、あえて SerializeField にしていない
	/// </summary>
	PlaneController plane;

	TrailRenderer trail;

	/// <summary>いまの濃さ（0〜1）</summary>
	float fade = 0f;

	/// <summary>
	/// 濃さを差し込む入れ物。
	/// 左右の軌跡は1枚のマテリアルを共有しているので、
	/// マテリアルの色を直接触ると両方が同時に変わってしまう
	/// </summary>
	MaterialPropertyBlock block;

	static readonly int Base_Color_Id = Shader.PropertyToID("_BaseColor");

	void Awake()
	{
		trail = GetComponent<TrailRenderer>();
		plane = GetComponentInParent<PlaneController>();

		// 出しっぱなしで始めると、ブーストしていないのに軌跡が伸びる
		trail.emitting = false;
		trail.Clear();

		block = new MaterialPropertyBlock();
		ApplyFade();
	}

	void LateUpdate()
	{
		if (plane == null)
		{
			return;
		}

		// 位置の更新後に判定する。Update で切り替えると、
		// 移動前の座標で点が打たれて軌跡が1フレームぶん遅れる
		bool isBoosting = plane.IsBoosting;
		trail.emitting = isBoosting;

		if (isBoosting == true)
		{
			// 踏み直したときは即座に戻す。ここを緩やかにすると操作への反応が鈍く感じる
			fade = 1f;
		}
		else if (0f < fade)
		{
			float step = fadeOutSeconds <= 0f ? 1f : Time.deltaTime / fadeOutSeconds;
			fade = Mathf.Max(0f, fade - step);
		}

		ApplyFade();
	}

	/// <summary>
	/// いまの濃さを描画へ反映する
	/// </summary>
	void ApplyFade()
	{
		trail.GetPropertyBlock(block);
		block.SetColor(Base_Color_Id, new Color(1f, 1f, 1f, fade));
		trail.SetPropertyBlock(block);
	}
}
