using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// バッチモードからAndroid向けAPKをビルドする。
/// エディターを起動したままの実行だとビルドが完了する前に打ち切られてしまうため、
/// -batchmode で Unity を起動して呼び出す。
///
/// 実行例:
///   Unity.exe -quit -batchmode -nographics -projectPath &lt;プロジェクト&gt;
///             -executeMethod AndroidBuilder.BuildAndroid
///             -buildOutput &lt;出力APKパス&gt; -logFile &lt;ログパス&gt;
/// </summary>
public static class AndroidBuilder
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Start_Marker = "HF2_BUILD_START";
	const string Done_Marker = "HF2_BUILD_DONE";

	public static void BuildAndroid()
	{
		string outputPath = GetCommandLineArg("-buildOutput");
		if (string.IsNullOrEmpty(outputPath) == true)
		{
			outputPath = GetDefaultOutputPath();
		}

		string outputDirectory = Path.GetDirectoryName(outputPath);
		if (string.IsNullOrEmpty(outputDirectory) == false && Directory.Exists(outputDirectory) == false)
		{
			Directory.CreateDirectory(outputDirectory);
		}

		List<string> scenes = new List<string>();
		foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
		{
			if (scene.enabled == true)
			{
				scenes.Add(scene.path);
			}
		}

		Debug.Log(Start_Marker + " scenes=" + scenes.Count + " out=" + outputPath);

		if (scenes.Count == 0)
		{
			Debug.LogError(Done_Marker + " result=NoScenes");
			EditorApplication.Exit(1);
			return;
		}

		// 必ずAPKで出す。エディターでApp Bundle（.aab）に切り替えたまま実行すると、
		// 出力パスの拡張子が.apkでも中身はAABになる。
		// AABは端末に直接インストールできず、Tools/deploygate-upload.ps1 も *.apk しか探さないため、
		// 配信まで通らない。ここで明示的に戻す
		EditorUserBuildSettings.buildAppBundle = false;

		BuildPlayerOptions options = new BuildPlayerOptions();
		options.scenes = scenes.ToArray();
		options.locationPathName = outputPath;
		options.target = BuildTarget.Android;
		options.targetGroup = BuildTargetGroup.Android;
		options.options = BuildOptions.None;

		BuildReport report = BuildPipeline.BuildPlayer(options);
		BuildSummary summary = report.summary;

		Debug.Log(Done_Marker
			+ " result=" + summary.result
			+ " errors=" + summary.totalErrors
			+ " warnings=" + summary.totalWarnings
			+ " sizeBytes=" + summary.totalSize
			+ " duration=" + summary.totalTime
			+ " path=" + summary.outputPath);

		if (summary.result != BuildResult.Succeeded)
		{
			EditorApplication.Exit(1);
			return;
		}

		EditorApplication.Exit(0);
	}

	/// <summary>
	/// 既定の出力先。リポジトリを汚さないよう、プロジェクトの兄弟フォルダに出す
	/// （Tools/deploygate-upload.ps1 が既定で見るフォルダと同じ）
	/// </summary>
	static string GetDefaultOutputPath()
	{
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string siblingDirectory = Path.Combine(Directory.GetParent(projectRoot).FullName, "HarryFly2_Build");
		return Path.Combine(siblingDirectory, "HarryFly2Build.apk");
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
