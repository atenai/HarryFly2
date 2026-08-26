using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 各ステージの開始地点を1枚ずつ画像に書き出す。
///
/// 空・フォグ・ライトを触ったときに、13ステージぶんを実際に開いて見比べるのは手間が掛かる。
/// バッチモードで一気に書き出して並べられるようにしておく。
///
/// 実行例（-nographics は付けないこと。描画できなくなる）:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod StageThumbnailer.CaptureAll
///             -thumbOutput &lt;出力フォルダ&gt; -logFile &lt;ログパス&gt;
/// </summary>
public static class StageThumbnailer
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_THUMB_DONE";

	/// <summary>書き出す画像の大きさ。実機と同じ縦長にする</summary>
	const int Width = 720;
	const int Height = 1280;

	public static void CaptureAll()
	{
		string outputDirectory = GetCommandLineArg("-thumbOutput");
		if (string.IsNullOrEmpty(outputDirectory) == true)
		{
			outputDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "StageThumbs");
		}
		Directory.CreateDirectory(outputDirectory);

		int captured = 0;

		for (int i = 0; i < 13; i++)
		{
			string scenePath = "Assets/HarryFly2/Scenes/Stage" + i + ".unity";
			if (File.Exists(scenePath) == false)
			{
				continue;
			}

			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

			Camera camera = FindSceneCamera();
			if (camera == null)
			{
				Debug.LogWarning("Stage" + i + ": カメラが見つかりません");
				continue;
			}

			string outputPath = Path.Combine(outputDirectory, "Stage" + i.ToString("00") + ".png");
			if (Capture(camera, outputPath) == true)
			{
				captured++;
				Debug.Log("Stage" + i + " -> " + outputPath);
			}
		}

		Debug.Log(Done_Marker + " captured=" + captured + " dir=" + outputDirectory);
		EditorApplication.Exit(0);
	}

	/// <summary>
	/// シーン内のカメラを探す。
	/// Camera.main はタグ付けと有効状態に依存するので、見つからなければ順に探す
	/// </summary>
	static Camera FindSceneCamera()
	{
		if (Camera.main != null)
		{
			return Camera.main;
		}

		Camera[] cameras = Object.FindObjectsOfType<Camera>();
		for (int i = 0; i < cameras.Length; i++)
		{
			if (cameras[i].isActiveAndEnabled == true)
			{
				return cameras[i];
			}
		}
		return null;
	}

	/// <summary>
	/// カメラの絵をPNGに書き出す
	/// </summary>
	/// <param name="camera">描画に使うカメラ</param>
	/// <param name="outputPath">出力先</param>
	static bool Capture(Camera camera, string outputPath)
	{
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

			File.WriteAllBytes(outputPath, texture.EncodeToPNG());
			Object.DestroyImmediate(texture);
			return true;
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
