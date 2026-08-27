using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 噴射炎の見た目を確認するための画像を書き出す。
///
/// 実機で確かめようとすると、ブーストは押している間しか出ないうえ、
/// 機体が毎秒300で飛んで壁に当たるため、撮りたい瞬間を狙えない。
/// ParticleSystem.Simulate() はエディタ上で任意の時刻まで進められるので、
/// 「噴射が安定した状態」を毎回同じ条件で撮れる。
///
/// 実行例（-nographics は付けないこと。描画できなくなる）:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod FlamePreview.Capture
///             -flameOutput &lt;出力フォルダ&gt; -logFile &lt;ログパス&gt;
/// </summary>
public static class FlamePreview
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_FLAMEPREVIEW_DONE";

	const int Width = 720;
	const int Height = 1280;

	/// <summary>噴射が安定するまで進める時間（秒）</summary>
	const float SimulateSeconds = 1.5f;

	public static void Capture()
	{
		string outputDirectory = GetCommandLineArg("-flameOutput");
		if (string.IsNullOrEmpty(outputDirectory) == true)
		{
			outputDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "FlamePreview");
		}
		Directory.CreateDirectory(outputDirectory);

		EditorSceneManager.OpenScene("Assets/HarryFly2/Scenes/Stage3.unity", OpenSceneMode.Single);

		PlaneController plane = Object.FindObjectOfType<PlaneController>();
		if (plane == null)
		{
			Debug.LogError("PlaneController が見つかりません");
			EditorApplication.Exit(1);
			return;
		}

		Transform flame = plane.transform.Find("EngineFlame");
		if (flame == null)
		{
			Debug.LogError("EngineFlame が機体に付いていません");
			EditorApplication.Exit(1);
			return;
		}

		Camera camera = Camera.main;
		if (camera == null)
		{
			Camera[] cameras = Object.FindObjectsOfType<Camera>();
			camera = cameras.Length > 0 ? cameras[0] : null;
		}
		if (camera == null)
		{
			Debug.LogError("カメラが見つかりません");
			EditorApplication.Exit(1);
			return;
		}

		// 機体モデルが両方出ていると重なるので、実行時と同じく1つだけ残す
		Transform model2 = plane.transform.Find("PlaneModel2");
		if (model2 != null)
		{
			model2.gameObject.SetActive(false);
		}

		DumpSettings(flame);

		// 噴射なし・噴射あり（通常速度）・噴射あり（最高速相当）の3枚
		Capture(camera, flame, outputDirectory, "Flame_Off", false, 1f);
		Capture(camera, flame, outputDirectory, "Flame_Min", true, 1f);
		Capture(camera, flame, outputDirectory, "Flame_Max", true, 2.2f);

		Debug.Log(Done_Marker + " dir=" + outputDirectory);
		EditorApplication.Exit(0);
	}

	/// <summary>
	/// 実際に効いている値を出す。
	/// 見た目がおかしいときに、どのモジュールが原因か推測せずに済むようにする
	/// </summary>
	static void DumpSettings(Transform flame)
	{
		Debug.Log("--- 実測値 ---");
		Debug.Log(string.Format("root: localScale={0} localPos={1} euler={2} lossyScale={3}",
			flame.localScale.ToString("F3"), flame.localPosition.ToString("F2"),
			flame.localEulerAngles.ToString("F0"), flame.lossyScale.ToString("F3")));

		ParticleSystem[] systems = flame.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < systems.Length; i++)
		{
			ParticleSystem.MainModule main = systems[i].main;
			ParticleSystem.ShapeModule shape = systems[i].shape;
			ParticleSystem.EmissionModule emission = systems[i].emission;
			ParticleSystemRenderer renderer = systems[i].GetComponent<ParticleSystemRenderer>();

			Debug.Log(string.Format(
				"{0}: space={1} scalingMode={2} life={3:F2} speed={4:F2} size={5:F2} max={6} rate={7:F0} gravity={8:F2}",
				systems[i].name, main.simulationSpace, main.scalingMode,
				main.startLifetimeMultiplier, main.startSpeedMultiplier, main.startSizeMultiplier,
				main.maxParticles, emission.rateOverTimeMultiplier, main.gravityModifierMultiplier));

			Debug.Log(string.Format(
				"   shape: enabled={0} type={1} radius={2:F2} angle={3:F1} scale={4} / renderMode={5} lengthScale={6:F2}",
				shape.enabled, shape.shapeType, shape.radius, shape.angle, shape.scale.ToString("F2"),
				renderer != null ? renderer.renderMode.ToString() : "?",
				renderer != null ? renderer.lengthScale : 0f));

			Debug.Log(string.Format("   velocityOverLifetime={0} noise={1} forceOverLifetime={2} inheritVelocity={3}",
				systems[i].velocityOverLifetime.enabled, systems[i].noise.enabled,
				systems[i].forceOverLifetime.enabled, systems[i].inheritVelocity.enabled));
		}
	}

	/// <summary>
	/// 1枚書き出す
	/// </summary>
	/// <param name="camera">描画に使うカメラ</param>
	/// <param name="flame">噴射炎</param>
	/// <param name="outputDirectory">出力先</param>
	/// <param name="name">ファイル名</param>
	/// <param name="active">噴射させるかどうか</param>
	/// <param name="lengthScale">奥行きの倍率。速度連動の再現に使う</param>
	static void Capture(Camera camera, Transform flame, string outputDirectory, string name, bool active, float lengthScale)
	{
		flame.gameObject.SetActive(active);

		if (active == true)
		{
			Vector3 scale = flame.localScale;
			// EngineFlame.cs が実行時にやっている拡大を、ここでは手で再現する
			flame.localScale = new Vector3(scale.x * lengthScale, scale.y * lengthScale, scale.z * lengthScale);

			ParticleSystem[] systems = flame.GetComponentsInChildren<ParticleSystem>(true);
			int alive = 0;
			for (int i = 0; i < systems.Length; i++)
			{
				// 親から呼ぶと子も一緒に進むので、ルートだけ Simulate する
				if (systems[i].transform == flame)
				{
					systems[i].Simulate(SimulateSeconds, true, true);
				}
			}
			for (int i = 0; i < systems.Length; i++)
			{
				alive = alive + systems[i].particleCount;
			}
			Debug.Log(name + ": 生存パーティクル数 = " + alive + " lengthScale=" + lengthScale);
		}

		RenderTexture renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
		RenderTexture previousTarget = camera.targetTexture;
		RenderTexture previousActive = RenderTexture.active;

		try
		{
			camera.targetTexture = renderTexture;
			camera.Render();

			RenderTexture.active = renderTexture;
			Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
			texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
			texture.Apply();

			string path = Path.Combine(outputDirectory, name + ".png");
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Object.DestroyImmediate(texture);
			Debug.Log("書き出し: " + path);
		}
		finally
		{
			camera.targetTexture = previousTarget;
			RenderTexture.active = previousActive;
			renderTexture.Release();
			Object.DestroyImmediate(renderTexture);
		}
	}

	static string GetCommandLineArg(string name)
	{
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == name)
			{
				return args[i + 1];
			}
		}
		return null;
	}
}
