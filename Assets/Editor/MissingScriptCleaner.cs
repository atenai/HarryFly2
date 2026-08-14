using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// スクリプト参照が切れた MonoBehaviour をシーンから取り除く。
/// 削除済みスクリプトのコンポーネントがシーンに残ると、
/// 「The referenced script (Unknown) on this Behaviour is missing!」の警告源になる。
///
/// 実行例:
///   Unity.exe -quit -batchmode -nographics -projectPath &lt;プロジェクト&gt;
///             -executeMethod MissingScriptCleaner.CleanBuildScenes -logFile &lt;ログパス&gt;
/// </summary>
public static class MissingScriptCleaner
{
	const string Marker = "HF2_CLEAN";

	/// <summary>
	/// Build Settings に登録された全シーンから参照切れコンポーネントを取り除いて保存する。
	/// 何も無いシーンは保存しない（無用な差分を出さないため）。
	/// </summary>
	public static void CleanBuildScenes()
	{
		List<string> scenePaths = new List<string>();
		foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
		{
			scenePaths.Add(entry.path);
		}

		int totalRemoved = 0;
		int totalObjects = 0;
		int changedScenes = 0;

		foreach (string scenePath in scenePaths)
		{
			Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			if (scene.IsValid() == false)
			{
				Debug.LogError(Marker + " OPEN_FAILED path=" + scenePath);
				continue;
			}

			int removedInScene = 0;
			int objectsInScene = 0;

			foreach (GameObject root in scene.GetRootGameObjects())
			{
				// 非アクティブなオブジェクトも対象にする
				Transform[] all = root.GetComponentsInChildren<Transform>(true);
				foreach (Transform t in all)
				{
					int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
					if (removed > 0)
					{
						removedInScene += removed;
						objectsInScene++;
					}
				}
			}

			if (removedInScene > 0)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				bool saved = EditorSceneManager.SaveScene(scene);
				changedScenes++;
				Debug.Log(Marker + " SCENE path=" + scenePath
					+ " removed=" + removedInScene
					+ " objects=" + objectsInScene
					+ " saved=" + saved);
			}
			else
			{
				Debug.Log(Marker + " SCENE path=" + scenePath + " removed=0 (変更なし)");
			}

			totalRemoved += removedInScene;
			totalObjects += objectsInScene;
		}

		Debug.Log(Marker + "_DONE totalRemoved=" + totalRemoved
			+ " totalObjects=" + totalObjects
			+ " changedScenes=" + changedScenes
			+ " scannedScenes=" + scenePaths.Count);

		AssetDatabase.SaveAssets();
		EditorApplication.Exit(0);
	}
}
