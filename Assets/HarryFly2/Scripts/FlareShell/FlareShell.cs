using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 照明弾1発。夜空をゆっくり上昇しながらレンズフレアを放ち、燃え尽きて消える。
///
/// 上昇と減光は Time.deltaTime で進める。ブースト中でもフレームレートが落ちても
/// 同じ速さで昇るようにするため。
/// </summary>
[RequireComponent(typeof(LensFlareComponentSRP))]
public class FlareShell : MonoBehaviour
{
	[Tooltip("上昇速度（1秒あたり）")]
	[SerializeField] float riseSpeed = 22f;

	[Tooltip("横に流れる速度（1秒あたり）。まっすぐ上がるだけだと打ち上げ花火のように見えない")]
	[SerializeField] float driftSpeed = 4f;

	[Tooltip("消えるまでの時間（秒）")]
	[SerializeField] float lifeSeconds = 10f;

	[Tooltip("点火してから最大の明るさになるまでの時間（秒）")]
	[SerializeField] float igniteSeconds = 0.5f;

	[Tooltip("消え始める時刻（寿命に対する割合）。ここから寿命まで徐々に暗くなる")]
	[SerializeField, Range(0.1f, 1f)] float fadeStartRatio = 0.55f;

	/// <summary>
	/// 最大時のフレアの強さ。
	/// 強くしすぎるとブルームが膨らんで画面を覆う白い塊になる。
	/// 同じシーンの太陽フレアが 0.30 で運用されているので、それに近い値に収めている
	/// </summary>
	[Tooltip("最大時のフレアの強さ。大きくしすぎるとブルームで画面が白飛びする")]
	[SerializeField] float maxFlareIntensity = 0.35f;

	[Tooltip("周囲を照らすライト。無くても動く")]
	[SerializeField] Light shellLight;

	[Tooltip("最大時のライトの明るさ")]
	[SerializeField] float maxLightIntensity = 3f;

	LensFlareComponentSRP flare;

	/// <summary>点火からの経過時間（秒）</summary>
	float elapsed = 0f;

	/// <summary>横に流れる向き。1発ごとに変えて、同じ動きに揃わないようにする</summary>
	Vector3 driftDirection = Vector3.zero;

	void Awake()
	{
		flare = GetComponent<LensFlareComponentSRP>();
		// 点いた状態で現れると打ち上げに見えないので、消灯から始める
		flare.intensity = 0f;
		if (shellLight != null)
		{
			shellLight.intensity = 0f;
		}

		float angle = Random.Range(0f, Mathf.PI * 2f);
		driftDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
	}

	void Update()
	{
		elapsed = elapsed + Time.deltaTime;

		if (lifeSeconds <= elapsed)
		{
			Destroy(this.gameObject);
			return;
		}

		this.transform.position = this.transform.position
			+ (Vector3.up * riseSpeed + driftDirection * driftSpeed) * Time.deltaTime;

		float brightness = GetBrightness();
		flare.intensity = maxFlareIntensity * brightness;
		if (shellLight != null)
		{
			shellLight.intensity = maxLightIntensity * brightness;
		}
	}

	/// <summary>
	/// いまの明るさ（0〜1）。点火で立ち上がり、後半で燃え尽きるように落とす
	/// </summary>
	float GetBrightness()
	{
		if (elapsed < igniteSeconds)
		{
			return igniteSeconds <= 0f ? 1f : elapsed / igniteSeconds;
		}

		float fadeStart = lifeSeconds * fadeStartRatio;
		if (elapsed < fadeStart)
		{
			return 1f;
		}

		float fadeDuration = lifeSeconds - fadeStart;
		if (fadeDuration <= 0f)
		{
			return 0f;
		}

		return 1f - (elapsed - fadeStart) / fadeDuration;
	}
}
