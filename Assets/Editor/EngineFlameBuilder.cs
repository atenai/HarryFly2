using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hovl Studio の Fire3 を元に、常時点いているエンジンの炎を組み立てる。
///
/// Fire3 は「その場で燃え続ける焚き火」として作られているので、
/// 上へ立ち上る向き・長い寿命・大きな粒という、
/// 毎秒300〜1500で飛ぶ機体には全く合わない設定になっている。
/// ここで機体の後方へ噴く短い炎に作り替える。
///
/// 実行例:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod EngineFlameBuilder.Build -logFile &lt;ログパス&gt;
/// </summary>
public static class EngineFlameBuilder
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_ENGINEFLAME_DONE";

	const string SourcePrefab = "Assets/Hovl Studio/3D Fire and Explosions/Prefabs/Fire3.prefab";
	const string OutputPrefab = "Assets/HarryFly2/Prefab/EngineFlame.prefab";
	const string MaterialDirectory = "Assets/HarryFly2/Material/EngineFlame";
	const string PlanePrefab = "Assets/HarryFly2/Prefab/Plane.prefab";

	/// <summary>
	/// 機体の後端。
	/// カメラは y=0.5 から見ていて炎は機体より手前にあるため、
	/// 機体と同じ高さに置くと遠近で画面上は下にずれて見える。その分を上げてある。
	/// 後ろへ出しすぎるとカメラと機体の間に入って画面を覆う
	/// </summary>
	static readonly Vector3 NozzlePosition = new Vector3(0f, 0.40f, -1.28f);

	/// <summary>
	/// Hovl の炎は自分の +Y へ立ち上る。機体の後ろへ噴かせたいので、
	/// X軸まわりに -90度 回して +Y を -Z（後方）へ向ける
	/// </summary>
	static readonly Vector3 NozzleEuler = new Vector3(-90f, 0f, 0f);

	/// <summary>
	/// 常時点いていて画面のど真ん中に出続けるので、控えめにする。
	/// 大きいと機体そのものが見えなくなる
	/// </summary>
	static readonly Vector3 NozzleScale = new Vector3(0.05f, 0.05f, 0.05f);

	public static void Build()
	{
		GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
		if (source == null)
		{
			Debug.LogError("元のプレハブが見つかりません: " + SourcePrefab);
			EditorApplication.Exit(1);
			return;
		}

		Directory.CreateDirectory(MaterialDirectory);

		GameObject flame = Object.Instantiate(source);
		flame.name = "EngineFlame";

		try
		{
			RemoveLights(flame);
			FixMaterials(flame);
			TuneSystems(flame);

			flame.transform.localPosition = NozzlePosition;
			flame.transform.localEulerAngles = NozzleEuler;
			flame.transform.localScale = NozzleScale;

			if (flame.GetComponent<EngineFlame>() == null)
			{
				flame.AddComponent<EngineFlame>();
			}

			PrefabUtility.SaveAsPrefabAsset(flame, OutputPrefab);
			Debug.Log("作成: " + OutputPrefab);
		}
		finally
		{
			Object.DestroyImmediate(flame);
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		AttachToPlane();

		Debug.Log(Done_Marker);
		EditorApplication.Exit(0);
	}

	/// <summary>
	/// 排気の色味。元テクスチャが黄緑寄りなので、橙側へ引っ張るために掛ける
	/// </summary>
	static readonly Color ExhaustTint = new Color(1f, 0.52f, 0.22f, 1f);

	/// <summary>
	/// そのプロパティを持っているマテリアルにだけ色を入れる。
	/// シェーダによって持っているプロパティが違うので、存在確認をしてから触る
	/// </summary>
	static void SetColorIfExists(Material material, string property, Color color)
	{
		if (material.HasProperty(property) == true)
		{
			material.SetColor(property, color);
			Debug.Log("   " + property + " = " + color);
		}
	}

	/// <summary>
	/// 実光源を消す。常時点けっぱなしにするので、これが残っていると負荷が乗り続ける
	/// </summary>
	static void RemoveLights(GameObject root)
	{
		Light[] lights = root.GetComponentsInChildren<Light>(true);
		for (int i = 0; i < lights.Length; i++)
		{
			Debug.Log("実光源を削除: " + lights[i].gameObject.name);
			Object.DestroyImmediate(lights[i].gameObject);
		}
	}

	/// <summary>
	/// マテリアルを複製して深度依存を切る。
	/// このプロジェクトは URP-Asset の m_RequireDepthTexture が 0 で深度テクスチャが無いため、
	/// _Usedepth を立てたままだと正しく描画されない
	/// </summary>
	static void FixMaterials(GameObject root)
	{
		Dictionary<Material, Material> remap = new Dictionary<Material, Material>();
		ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);

		for (int i = 0; i < renderers.Length; i++)
		{
			Material original = renderers[i].sharedMaterial;
			if (original == null)
			{
				continue;
			}

			Material copy;
			if (remap.TryGetValue(original, out copy) == false)
			{
				copy = new Material(original);
				copy.name = original.name + " (EngineFlame)";
				if (copy.HasProperty("_Usedepth") == true)
				{
					copy.SetFloat("_Usedepth", 0f);
				}

				// 色はパーティクル側の指定だけでは変わらない。
				// Hovl のシェーダはマテリアルの色を掛けて最終的な見た目を決めるので、
				// 元テクスチャの黄緑寄りをここで橙へ引っ張る
				SetColorIfExists(copy, "_Color", ExhaustTint);
				SetColorIfExists(copy, "_GlowColor", ExhaustTint);
				SetColorIfExists(copy, "_EmissionColor", ExhaustTint);
				SetColorIfExists(copy, "_TintColor", new Color(ExhaustTint.r * 0.5f, ExhaustTint.g * 0.5f, ExhaustTint.b * 0.5f, 0.5f));

				string path = Path.Combine(MaterialDirectory, SanitizeFileName(copy.name) + ".mat").Replace("\\", "/");
				AssetDatabase.CreateAsset(copy, path);
				remap.Add(original, copy);
				Debug.Log("マテリアル複製: " + original.name + " -> " + path + " (_Usedepth=0)");
			}

			renderers[i].sharedMaterial = copy;
		}
	}

	/// <summary>
	/// 焚き火の設定を、高速で飛ぶ機体の排気に作り替える
	/// </summary>
	static void TuneSystems(GameObject root)
	{
		ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < systems.Length; i++)
		{
			ParticleSystem system = systems[i];
			ParticleSystem.MainModule main = system.main;

			// World のままだと、毎秒300〜1500で進む機体から粒子が置き去りにされ、
			// ノズルから離れた場所に糸を引くだけになる
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.loop = true;
			main.playOnAwake = true;
			// 焚き火は寿命3秒だが、排気は一瞬で流れ去る
			main.startLifetimeMultiplier = 0.3f;
			// 常時出るので数を絞る。画面中央の重なりは描画コストに直結する
			main.maxParticles = 24;
			// 上へ立ち上る勢いを、後方へ抜ける速さに変える
			main.startSpeedMultiplier = 2.2f;
			main.gravityModifierMultiplier = 0f;

			// Fire3 は radius 4.0 の広いコーンから炎を上げる焚き火なので、
			// そのままだと機体と同じ幅に粒が散らばる。
			// ノズルは点に近いので、発生範囲を絞って細いコーンにする
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Cone;
			shape.radius = 0.4f;
			shape.angle = 10f;

			// 粒の大きさも絞る。カメラは機体の1.05ユニット手前に居るので、
			// このスケールでは 1 ユニットの粒でも画面上で100px規模になる
			main.startSizeMultiplier = 1.5f;

			ParticleSystem.EmissionModule emission = system.emission;
			emission.rateOverTimeMultiplier = 45f;
			emission.rateOverDistanceMultiplier = 0f;

			// 噴射口で太く、離れるほど細くする
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, BuildTaperCurve());

			// Fire3 の元の色は焚き火の黄緑寄りで、エンジンの排気には見えない。
			// 噴射口は白熱、離れるにつれ橙から暗い赤へ落とす
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildExhaustGradient());

			ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
			if (renderer != null)
			{
				renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				renderer.receiveShadows = false;
				// 速度で引き伸ばされる描画モードは、この速度域では画面を貫く線になる
				if (renderer.renderMode == ParticleSystemRenderMode.Stretch)
				{
					renderer.renderMode = ParticleSystemRenderMode.Billboard;
					Debug.Log("Stretched Billboard を通常のビルボードに変更: " + system.name);
				}
			}

			Debug.Log(string.Format("調整: {0} life={1:F2} rate={2:F0} max={3} speed={4:F1} space=Local",
				system.name, main.startLifetimeMultiplier, emission.rateOverTimeMultiplier,
				main.maxParticles, main.startSpeedMultiplier));
		}
	}

	/// <summary>
	/// 排気の色の移り変わり。
	/// アルファを最後に0まで落とさないと、粒子が消える瞬間に四角く途切れて見える
	/// </summary>
	static Gradient BuildExhaustGradient()
	{
		Gradient gradient = new Gradient();

		GradientColorKey[] colors =
		{
			new GradientColorKey(new Color(1f, 0.95f, 0.82f), 0f),
			new GradientColorKey(new Color(1f, 0.60f, 0.18f), 0.4f),
			new GradientColorKey(new Color(0.55f, 0.14f, 0.03f), 1f),
		};

		GradientAlphaKey[] alphas =
		{
			new GradientAlphaKey(0.9f, 0f),
			new GradientAlphaKey(0.7f, 0.5f),
			new GradientAlphaKey(0f, 1f),
		};

		gradient.SetKeys(colors, alphas);
		return gradient;
	}

	static AnimationCurve BuildTaperCurve()
	{
		AnimationCurve curve = new AnimationCurve();
		curve.AddKey(0f, 1f);
		curve.AddKey(0.4f, 0.7f);
		curve.AddKey(1f, 0.05f);
		return curve;
	}

	/// <summary>
	/// 機体へ取り付ける。
	/// ブーストの噴射（Particle System）はそのまま残す。あちらは加速中だけの演出で、
	/// こちらは常時点いているエンジンなので役割が違う
	/// </summary>
	static void AttachToPlane()
	{
		GameObject flamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefab);
		if (flamePrefab == null)
		{
			Debug.LogError("作った炎プレハブを読み込めません: " + OutputPrefab);
			return;
		}

		GameObject root = PrefabUtility.LoadPrefabContents(PlanePrefab);
		try
		{
			Transform existing = root.transform.Find("EngineFlame");
			if (existing != null)
			{
				Object.DestroyImmediate(existing.gameObject);
			}

			GameObject flame = (GameObject)PrefabUtility.InstantiatePrefab(flamePrefab, root.transform);
			flame.name = "EngineFlame";
			flame.transform.localPosition = NozzlePosition;
			flame.transform.localEulerAngles = NozzleEuler;
			flame.transform.localScale = NozzleScale;
			// 常時点いているので、最初から有効にしておく
			flame.SetActive(true);

			PrefabUtility.SaveAsPrefabAsset(root, PlanePrefab);
			Debug.Log("機体に取り付け: EngineFlame");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}

		// 保存し直したものを読んで確認する
		GameObject check = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefab);
		Transform attached = check.transform.Find("EngineFlame");
		Transform boost = check.transform.Find("Particle System");
		Debug.Log("verify: EngineFlame=" + (attached != null ? "あり" : "なし")
			+ " / ブーストの Particle System=" + (boost != null ? "あり（維持）" : "なし"));
	}

	static string SanitizeFileName(string name)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		for (int i = 0; i < invalid.Length; i++)
		{
			name = name.Replace(invalid[i], '_');
		}
		return name;
	}
}
