using UnityEngine;

/// <summary>
/// エンジンの炎。飛んでいる間はずっと点いている。
///
/// ブーストの噴射（PlaneController.paticlePrefab）とは別物で、
/// あちらは加速中だけ出る演出、こちらは常時点いているエンジンそのもの。
///
/// 墜落後とゴール後は消す。HidePlaneModel() が消すのは planePrefabs だけなので、
/// 機体の子に足したこの炎は自分で止めないと、
/// 撃墜されて機体が消えたあとも炎だけ宙に残る。
/// </summary>
public class EngineFlame : MonoBehaviour
{
	/// <summary>
	/// ブースト中に炎を強める倍率。
	/// 1のままだと加速しても炎が変わらず、常時演出と区別がつかない
	/// </summary>
	[Tooltip("最高速のときの炎の大きさの倍率")]
	[SerializeField] float boostSizeMultiplier = 1.6f;

	[Tooltip("最高速のときの噴出量の倍率")]
	[SerializeField] float boostEmissionMultiplier = 1.5f;

	/// <summary>
	/// 見た目が速度に追いつく速さ（1秒あたり）。
	/// 速度へ即座に追従させると、加速の揺らぎがそのまま炎のばたつきになる
	/// </summary>
	[Tooltip("見た目が速度に追いつく速さ。小さいほどゆっくり変化する")]
	[SerializeField] float followSpeed = 3.5f;

	/// <summary>
	/// 状態の取得元。機体の子に置く前提なので親から辿る。
	/// BoostTrail と同じ作りにして、インスペクタでの割り当て漏れを防ぐ
	/// </summary>
	PlaneController plane;

	/// <summary>この炎が持っているパーティクル。毎フレーム取得し直さない</summary>
	ParticleSystem[] systems;

	/// <summary>元の噴出量と大きさ。倍率はここに掛ける</summary>
	float[] baseEmissionRates;
	float[] baseSizes;

	/// <summary>いま反映している加速の割合</summary>
	float currentRatio = 0f;

	/// <summary>いま噴出させているかどうか。切り替わったときだけ止め／再開する</summary>
	bool isEmitting = true;

	void Awake()
	{
		plane = GetComponentInParent<PlaneController>();
		systems = GetComponentsInChildren<ParticleSystem>(true);

		baseEmissionRates = new float[systems.Length];
		baseSizes = new float[systems.Length];
		for (int i = 0; i < systems.Length; i++)
		{
			baseEmissionRates[i] = systems[i].emission.rateOverTimeMultiplier;
			baseSizes[i] = systems[i].main.startSizeMultiplier;
		}
	}

	void LateUpdate()
	{
		if (plane == null)
		{
			return;
		}

		// 墜落後・ゴール後は消す。機体が消えているのに炎だけ残らないようにする。
		//
		// SetActive(false) では止めない。自分を無効にすると LateUpdate も止まり、
		// 二度と自力で戻れなくなる。いまは墜落後にシーンごと作り直されるので
		// 表面化しないが、あとで「復活」のような仕組みを足したときに嵌る。
		// パーティクルの発生だけを止めれば、この処理は動き続けられる
		bool shouldEmit = plane.IsFlying;
		if (shouldEmit != isEmitting)
		{
			isEmitting = shouldEmit;
			for (int i = 0; i < systems.Length; i++)
			{
				if (systems[i] == null)
				{
					continue;
				}
				if (shouldEmit == true)
				{
					systems[i].Play(false);
				}
				else
				{
					// 残っている粒子ごと消す。撃墜の瞬間に炎だけ宙に残らないようにする
					systems[i].Clear(false);
					systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
				}
			}
		}

		if (shouldEmit == false)
		{
			return;
		}

		float target = plane.IsBoosting == true ? plane.GetBoostSpeedRatio() : 0f;
		currentRatio = Mathf.MoveTowards(currentRatio, target, followSpeed * Time.deltaTime);

		float sizeMultiplier = Mathf.Lerp(1f, boostSizeMultiplier, currentRatio);
		float emissionMultiplier = Mathf.Lerp(1f, boostEmissionMultiplier, currentRatio);

		for (int i = 0; i < systems.Length; i++)
		{
			if (systems[i] == null)
			{
				continue;
			}

			ParticleSystem.MainModule main = systems[i].main;
			main.startSizeMultiplier = baseSizes[i] * sizeMultiplier;

			ParticleSystem.EmissionModule emission = systems[i].emission;
			emission.rateOverTimeMultiplier = baseEmissionRates[i] * emissionMultiplier;
		}
	}
}
