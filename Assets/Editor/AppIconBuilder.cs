using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// アプリのアイコンを、ゲーム本体の機体モデルから描き起こす。
///
/// 絵を外から持ち込むと、機体を作り替えるたびにアイコンだけ古いままになる。
/// 実際のプレハブを撮って作れば、見た目が食い違わない。
///
/// 実行例:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod AppIconBuilder.Build -logFile &lt;ログパス&gt;
///
/// -nographics は付けないこと。カメラで描画するため、グラフィックスが要る
/// </summary>
public static class AppIconBuilder
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_APPICON_DONE";

	const string PlanePrefabPath = "Assets/HarryFly2/Prefab/Plane.prefab";
	const string IconDirectory = "Assets/HarryFly2/Icon";
	const string IconPath = "Assets/HarryFly2/Icon/AppIcon.png";

	/// <summary>
	/// 書き出す大きさ。
	/// Android の最大のアイコン枠は 192、Play ストア用は 512 なので、
	/// 縮小して使えるよう 1024 で作っておく
	/// </summary>
	const int Size = 1024;

	/// <summary>夜空の色。カメラの背景色にも使う</summary>
	static readonly Color SkyTop = new Color(0.03f, 0.05f, 0.14f);
	static readonly Color SkyMiddle = new Color(0.20f, 0.13f, 0.28f);
	static readonly Color SkyBottom = new Color(0.78f, 0.34f, 0.10f);

	public static void Build()
	{
		string result = Render();
		Debug.Log(Done_Marker + " " + result);
		EditorApplication.Exit(0);
	}

	[MenuItem("Tools/HarryFly2/アプリアイコンを作る")]
	public static void BuildFromMenu()
	{
		Debug.Log(Render());
	}

	static string Render()
	{
		var planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
		if (planePrefab == null) { return "失敗: " + PlanePrefabPath + " が読み込めません"; }

		// ステージが開いたままだと背景に写り込むので、空のシーンで組む
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		var root = new GameObject("__IconRig");
		Texture2D shot = null;

		try
		{
			BuildBackground(root);
			GameObject plane = BuildPlane(root, planePrefab);
			BuildGlow(plane);
			BuildLight(root);
			shot = Capture(root, plane);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}

		if (shot == null) { return "失敗: 描画できませんでした"; }

		Directory.CreateDirectory(IconDirectory);
		File.WriteAllBytes(IconPath, shot.EncodeToPNG());
		Object.DestroyImmediate(shot);

		AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
		ConfigureImporter(IconPath);

		var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
		if (texture == null) { return "失敗: 書き出した画像を読み込めません"; }

		string assigned = AssignToAndroid(texture);

		AssetDatabase.SaveAssets();
		return "作成: " + IconPath + " (" + Size + "x" + Size + ") / " + assigned;
	}

	/// <summary>
	/// 夜空から地平の残光へのグラデーションを板で敷く。
	/// アイコンは小さく表示されるので、背景は単純な色の流れにして機体の輪郭を立たせる
	/// </summary>
	static void BuildBackground(GameObject root)
	{
		var gradient = new Texture2D(2, 256, TextureFormat.RGBA32, false);
		gradient.wrapMode = TextureWrapMode.Clamp;
		// 残光は下端の四半分に抑える。
		// 以前は画面の半分以上が明るい橙で、そちらに目が行って機体が主役でなくなっていた
		const float GlowBand = 0.26f;
		for (int y = 0; y < 256; y++)
		{
			float v = y / 255f;
			Color c = v < GlowBand
				? Color.Lerp(SkyBottom, SkyMiddle, v / GlowBand)
				: Color.Lerp(SkyMiddle, SkyTop, (v - GlowBand) / (1f - GlowBand));
			gradient.SetPixel(0, y, c);
			gradient.SetPixel(1, y, c);
		}
		gradient.Apply();

		var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
		material.SetTexture("_BaseMap", gradient);
		material.SetColor("_BaseColor", Color.white);

		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		Object.DestroyImmediate(quad.GetComponent<Collider>());
		quad.name = "Background";
		quad.transform.SetParent(root.transform, false);
		quad.GetComponent<MeshRenderer>().sharedMaterial = material;
		// 位置と大きさはカメラが決まってから合わせる。
		// ここで固定の大きさにすると、カメラが寄ったときに板の中央しか写らず、
		// グラデーションの端（地平の残光）が画面の外に出てしまう
	}

	/// <summary>
	/// 背景の板を、カメラの視界をちょうど覆う位置と大きさに置く。
	/// これでグラデーションの端から端までが必ず画面に収まる
	/// </summary>
	/// <param name="camera">合わせる相手のカメラ</param>
	static void FitBackground(GameObject root, Camera camera)
	{
		Transform quad = root.transform.Find("Background");
		if (quad == null) { return; }

		// 機体より確実に後ろへ置く
		const float Distance = 25f;
		float halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
		// 少し大きめにして、端に背景色の隙間が出ないようにする
		float height = 2f * Distance * Mathf.Tan(halfFov) * 1.05f;

		Transform cameraTransform = camera.transform;
		quad.position = cameraTransform.position + cameraTransform.forward * Distance;
		quad.rotation = cameraTransform.rotation;
		quad.localScale = new Vector3(height, height, 1f);
	}

	/// <summary>
	/// 機体を置く。ショップ用の予備モデルや軌跡は消して、1機だけ見せる
	/// </summary>
	static GameObject BuildPlane(GameObject root, GameObject prefab)
	{
		var plane = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
		foreach (Transform child in plane.transform)
		{
			if (child.name == "PlaneModel1") { continue; }
			child.gameObject.SetActive(false);
		}

		// 斜め前上から見下ろす角度。真横や真正面だと機体が板に見える。
		// 大きさと位置はカメラ側で合わせるので、ここでは触らない
		plane.transform.position = Vector3.zero;
		plane.transform.rotation = Quaternion.Euler(12f, -145f, -12f);
		plane.transform.localScale = Vector3.one;
		return plane;
	}

	/// <summary>
	/// 機体の見た目が占める範囲を求める。カメラの寄りを決めるのに使う
	/// </summary>
	static bool TryGetVisualBounds(GameObject target, out Bounds bounds)
	{
		bounds = new Bounds();
		bool found = false;
		foreach (var renderer in target.GetComponentsInChildren<Renderer>(false))
		{
			if (found == false) { bounds = renderer.bounds; found = true; }
			else { bounds.Encapsulate(renderer.bounds); }
		}
		return found;
	}

	/// <summary>
	/// 噴射口の小さな光。
	///
	/// 以前は大きな球を3つ重ねていたが、単色の球はただの塗り潰しにしかならず、
	/// 機体長の3割を覆う黄色い塊になって尾翼を隠していた。
	/// 光は「あることが分かる」程度に留め、輪郭は後ろからの光で立たせる
	/// </summary>
	static void BuildGlow(GameObject plane)
	{
		var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
		material.SetColor("_BaseColor", new Color(5f, 1.9f, 0.45f, 1f));

		var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Object.DestroyImmediate(glow.GetComponent<Collider>());
		glow.name = "Glow";
		glow.transform.SetParent(plane.transform, false);
		// 機体モデルの後端は z=-0.74。その少し後ろが噴射口にあたる
		glow.transform.localPosition = new Vector3(0f, 0.30f, -0.80f);
		glow.transform.localScale = Vector3.one * 0.13f;
		glow.GetComponent<MeshRenderer>().sharedMaterial = material;
	}

	/// <summary>
	/// 主光源に加えて、後ろから当てる光を入れる。
	/// 機体は灰色一色なので、輪郭に光の縁ができないと背景に溶ける
	/// </summary>
	static void BuildLight(GameObject root)
	{
		var keyObject = new GameObject("KeyLight");
		keyObject.transform.SetParent(root.transform, false);
		var key = keyObject.AddComponent<Light>();
		key.type = LightType.Directional;
		key.intensity = 2.2f;
		key.color = new Color(1f, 0.93f, 0.85f);
		keyObject.transform.rotation = Quaternion.Euler(35f, -20f, 0f);

		var rimObject = new GameObject("RimLight");
		rimObject.transform.SetParent(root.transform, false);
		var rim = rimObject.AddComponent<Light>();
		rim.type = LightType.Directional;
		rim.intensity = 1.1f;
		// 地平の残光と同じ色にして、背景と光の向きを一致させる
		rim.color = new Color(1f, 0.62f, 0.35f);
		rimObject.transform.rotation = Quaternion.Euler(-15f, 150f, 0f);
	}

	/// <summary>
	/// カメラを置いて正方形で撮る。
	///
	/// 寄りは機体の見た目の範囲から計算する。決め打ちの距離だと、
	/// 機体モデルを差し替えたときに主翼が画面外へはみ出す。
	///
	/// Android のアダプティブアイコンは円形に切り抜かれるため、
	/// 画面いっぱいに寄せてはいけない。中央の6割ほどに収める
	/// </summary>
	/// <param name="plane">画角を合わせる相手</param>
	static Texture2D Capture(GameObject root, GameObject plane)
	{
		const float FieldOfView = 32f;
		// 機体を画面の高さの何割に収めるか。
		//
		// measure しているのはワールド軸に沿った境界ボックスの角なので、
		// 斜めに構えた機体の実際のシルエットより2割ほど大きく出る。
		// 指定 0.60 のとき実測では画面の45%しか占めなかったため、その分を見込んでいる。
		// 上げすぎるとアダプティブアイコンの円形マスク（全体の約61%）で主翼が切れる
		const float FillRatio = 0.78f;

		var cameraObject = new GameObject("IconCamera");
		cameraObject.transform.SetParent(root.transform, false);
		var camera = cameraObject.AddComponent<Camera>();
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = SkyTop;
		camera.fieldOfView = FieldOfView;
		camera.nearClipPlane = 0.05f;
		camera.farClipPlane = 60f;

		// 見下ろす角度は固定して、距離だけを機体の大きさから決める
		cameraObject.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

		Bounds bounds;
		if (TryGetVisualBounds(plane, out bounds) == true)
		{
			// 画面に写る大きさで測る。
			// 境界球の半径（対角線）で測ると、飛行機のように平たく横長な形では
			// 実際の見た目よりはるかに大きく見積もられ、必要以上に引いてしまう
			Quaternion look = cameraObject.transform.rotation;
			Vector3 center = bounds.center;
			Vector3 extents = bounds.extents;

			float halfWidth = 0f;
			float halfHeight = 0f;
			for (int i = 0; i < 8; i++)
			{
				var corner = new Vector3(
					(i & 1) == 0 ? -extents.x : extents.x,
					(i & 2) == 0 ? -extents.y : extents.y,
					(i & 4) == 0 ? -extents.z : extents.z);
				// カメラから見た向きに直してから、縦横の広がりを取る
				Vector3 viewed = Quaternion.Inverse(look) * corner;
				halfWidth = Mathf.Max(halfWidth, Mathf.Abs(viewed.x));
				halfHeight = Mathf.Max(halfHeight, Mathf.Abs(viewed.y));
			}

			float half = Mathf.Max(halfWidth, halfHeight);
			float halfFov = FieldOfView * 0.5f * Mathf.Deg2Rad;
			float distance = half / (FillRatio * Mathf.Tan(halfFov));

			Vector3 direction = look * Vector3.forward;
			cameraObject.transform.position = center - direction * distance;
			Debug.Log("画角: 見た目の半幅 " + halfWidth.ToString("N2") + " 半高 " + halfHeight.ToString("N2")
				+ " / 距離 " + distance.ToString("N2"));
		}
		else
		{
			cameraObject.transform.position = new Vector3(0f, 0.35f, -4.5f);
		}

		// 背景をカメラの視界に合わせてから撮る
		FitBackground(root, camera);

		var renderTexture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
		renderTexture.antiAliasing = 8;
		camera.targetTexture = renderTexture;
		camera.Render();

		RenderTexture.active = renderTexture;
		var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
		texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
		texture.Apply();
		RenderTexture.active = null;

		camera.targetTexture = null;
		Object.DestroyImmediate(renderTexture);
		return texture;
	}

	/// <summary>
	/// アイコンは縮小して使われるので、圧縮を切って輪郭を潰さない
	/// </summary>
	static void ConfigureImporter(string path)
	{
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (importer == null) { return; }

		importer.textureType = TextureImporterType.Default;
		importer.mipmapEnabled = false;
		importer.alphaIsTransparency = false;
		importer.npotScale = TextureImporterNPOTScale.None;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
		importer.maxTextureSize = Size;
		importer.SaveAndReimport();
	}

	/// <summary>
	/// Android のアイコン枠すべてに同じ絵を入れる。
	/// Unity がビルド時に各解像度へ縮小する。
	///
	/// 枠の種類（Legacy / Adaptive / Round）は Android のプラットフォーム拡張側で定義されていて、
	/// 標準のエディタアセンブリからは名前で参照できない。
	/// 対応している種類を問い合わせる API を使えば、その参照なしで全種類を扱える
	/// </summary>
	static string AssignToAndroid(Texture2D texture)
	{
		try
		{
			// 対応する枠の問い合わせだけは、まだ BuildTargetGroup を取る古い形しかない
			PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android);
			if (kinds == null || kinds.Length == 0) { return "Android のアイコン枠が取得できませんでした"; }

			var names = new System.Text.StringBuilder();
			int filled = 0;

			foreach (var kind in kinds)
			{
				PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
				for (int i = 0; i < icons.Length; i++)
				{
					// 前景・背景の層を持つ枠があるので、層の数だけ同じ絵を入れる
					var layers = new Texture2D[icons[i].maxLayerCount];
					for (int layer = 0; layer < layers.Length; layer++) { layers[layer] = texture; }
					icons[i].SetTextures(layers);
					filled++;
				}
				PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);

				if (names.Length > 0) { names.Append(" / "); }
				names.Append(kind.ToString()).Append(" ").Append(icons.Length).Append("枠");
			}

			return "Android のアイコン枠 " + filled + " 箇所に設定（" + names.ToString() + "）";
		}
		catch (System.Exception exception)
		{
			return "アイコン枠への割り当てに失敗: " + exception.Message;
		}
	}
}
