using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 太陽のレンズフレアを、間に建物が入ったときに消す。
///
/// URP標準の遮蔽判定（LensFlareComponentSRP.useOcclusion）は深度テクスチャを使うが、
/// 空を背景にした遠方の太陽では常に「隠れている」と判定されてフレアが出なくなる。
/// ここではカメラから太陽へ Raycast を1本飛ばすだけの判定に置き換えている。
/// 深度テクスチャが要らないぶんモバイルでの負荷も軽い。
/// </summary>
[RequireComponent(typeof(LensFlareComponentSRP))]
public class SunLensFlareOcclusion : MonoBehaviour
{
	[Tooltip("遮られていないときのフレアの強さ")]
	[SerializeField] float visibleIntensity = 1.1f;

	[Tooltip("表示・非表示が切り替わる速さ（1秒あたりの強さの変化量）。大きいほどパッと切り替わる")]
	[SerializeField] float fadeSpeed = 5f;

	[Tooltip("フレアを遮る対象のレイヤー")]
	[SerializeField] LayerMask blockingLayers = ~0;

	LensFlareComponentSRP flare;

	/// <summary>いまの強さ。目標値へ徐々に近づける</summary>
	float currentIntensity = 0f;

	/// <summary>毎フレーム探し直さないように覚えておく</summary>
	Camera cachedCamera;

	void Awake()
	{
		flare = GetComponent<LensFlareComponentSRP>();
		// 出しっぱなしで始めると、初回に建物越しでも一瞬見えてしまう
		currentIntensity = 0f;
		flare.intensity = 0f;
	}

	void LateUpdate()
	{
		if (cachedCamera == null)
		{
			cachedCamera = Camera.main;
			if (cachedCamera == null)
			{
				return;
			}
		}

		Vector3 cameraPosition = cachedCamera.transform.position;
		Vector3 toSun = this.transform.position - cameraPosition;
		float distance = toSun.magnitude;
		if (distance <= 0.001f)
		{
			return;
		}

		// アイテムのコライダーはトリガーなので、拾う判定でフレアが消えないように除外する
		bool isBlocked = Physics.Raycast(cameraPosition, toSun / distance, distance, blockingLayers, QueryTriggerInteraction.Ignore);

		float target = isBlocked ? 0f : visibleIntensity;
		// 建物の縁をかすめるたびに点滅しないよう、少し時間をかけて切り替える
		currentIntensity = Mathf.MoveTowards(currentIntensity, target, fadeSpeed * Time.deltaTime);
		flare.intensity = currentIntensity;
	}
}
