using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 切り出した効果音を、対応するプレハブのフィールドへ割り当てる。
///
/// 手作業でインスペクタにドラッグすると、プレハブが増えたときに付け忘れが出る。
/// 割り当て先を一覧にして、実行後に読み直して確認するところまで自動でやる。
///
/// 実行例:
///   Unity.exe -quit -batchmode -nographics -projectPath &lt;プロジェクト&gt;
///             -executeMethod AudioAssignTool.AssignAll -logFile &lt;ログパス&gt;
/// </summary>
public static class AudioAssignTool
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_ASSIGN_DONE";

	const string AudioDirectory = "Assets/HarryFly2/Audio/";

	/// <summary>
	/// 割り当て1件ぶんの指定
	/// </summary>
	class Assignment
	{
		public string prefabPath;
		/// <summary>フィールド名 -> 効果音の名前</summary>
		public Dictionary<string, string> fields;
	}

	static readonly Assignment[] Assignments =
	{
		new Assignment
		{
			prefabPath = "Assets/HarryFly2/Prefab/Plane.prefab",
			fields = new Dictionary<string, string>
			{
				{ "goalSound", "Goal" },
				{ "fuelEmptySound", "Alarm" },
				{ "nearMissSound", "BulletPassBy" },
				{ "obstacleImpactSound", "ObstacleImpact" },
				{ "shotDownSound", "ShotDown" },
			},
		},
		new Assignment
		{
			prefabPath = "Assets/HarryFly2/Prefab/GameManager.prefab",
			fields = new Dictionary<string, string>
			{
				{ "timeUpSound", "TimeUp" },
			},
		},
		new Assignment
		{
			prefabPath = "Assets/HarryFly2/Prefab/StageManager.prefab",
			fields = new Dictionary<string, string>
			{
				{ "transitionSound", "StageTransition" },
			},
		},
		new Assignment
		{
			prefabPath = "Assets/HarryFly2/Prefab/AntiAirGun.prefab",
			fields = new Dictionary<string, string>
			{
				{ "fireSound", "AntiAirGunFire" },
			},
		},
		new Assignment
		{
			prefabPath = "Assets/HarryFly2/Prefab/Canvas.prefab",
			fields = new Dictionary<string, string>
			{
				{ "clickSound", "UIClick" },
				{ "openSound", "UIOpen" },
				{ "closeSound", "UIClose" },
				{ "countdownTickSound", "CountdownTick" },
				{ "fuelWarningSound", "FuelWarning" },
			},
		},
	};

	/// <summary>
	/// シーンに直接置かれているものへの割り当て。
	/// 照明弾のランチャーはプレハブ化されておらず、Stage1〜5 のシーンに直接ある
	/// </summary>
	static readonly Dictionary<string, string> SceneFields = new Dictionary<string, string>
	{
		{ "launchSound", "FlareLaunch" },
	};

	public static void AssignAll()
	{
		int assigned = 0;
		int missing = 0;

		for (int i = 0; i < Assignments.Length; i++)
		{
			Assignment a = Assignments[i];
			GameObject root = PrefabUtility.LoadPrefabContents(a.prefabPath);
			if (root == null)
			{
				Debug.LogError("プレハブが見つかりません: " + a.prefabPath);
				missing++;
				continue;
			}

			try
			{
				bool changed = false;
				foreach (KeyValuePair<string, string> pair in a.fields)
				{
					AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + pair.Value + ".wav");
					if (clip == null)
					{
						Debug.LogError("効果音が見つかりません: " + pair.Value);
						missing++;
						continue;
					}

					if (SetField(root, pair.Key, clip) == true)
					{
						Debug.Log(System.IO.Path.GetFileName(a.prefabPath) + "." + pair.Key + " = " + pair.Value);
						assigned++;
						changed = true;
					}
					else
					{
						Debug.LogError("フィールドが見つかりません: " + a.prefabPath + " -> " + pair.Key);
						missing++;
					}
				}

				if (changed == true)
				{
					PrefabUtility.SaveAsPrefabAsset(root, a.prefabPath);
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		assigned = assigned + AssignInScenes();

		AssetDatabase.SaveAssets();

		// 保存し直したものを読んで、参照が生きているか確かめる
		Debug.Log("--- verify ---");
		for (int i = 0; i < Assignments.Length; i++)
		{
			Assignment a = Assignments[i];
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(a.prefabPath);
			if (prefab == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, string> pair in a.fields)
			{
				Object value = GetField(prefab, pair.Key);
				Debug.Log("  " + System.IO.Path.GetFileName(a.prefabPath) + "." + pair.Key
					+ " = " + (value != null ? value.name : "(null)"));
			}
		}

		Debug.Log(Done_Marker + " assigned=" + assigned + " missing=" + missing);
		EditorApplication.Exit(missing == 0 ? 0 : 1);
	}

	/// <summary>
	/// 全ステージのシーンを開いて、シーン内に直接置かれているものへ割り当てる
	/// </summary>
	/// <returns>割り当てた件数</returns>
	static int AssignInScenes()
	{
		int assigned = 0;

		for (int i = 0; i < 13; i++)
		{
			string scenePath = "Assets/HarryFly2/Scenes/Stage" + i + ".unity";
			if (System.IO.File.Exists(scenePath) == false)
			{
				continue;
			}

			var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
				scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

			bool changed = false;
			GameObject[] roots = scene.GetRootGameObjects();
			foreach (KeyValuePair<string, string> pair in SceneFields)
			{
				AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + pair.Value + ".wav");
				if (clip == null)
				{
					continue;
				}

				for (int r = 0; r < roots.Length; r++)
				{
					if (SetField(roots[r], pair.Key, clip) == true)
					{
						Debug.Log("Stage" + i + "." + pair.Key + " = " + pair.Value);
						assigned++;
						changed = true;
						break;
					}
				}
			}

			if (changed == true)
			{
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
				UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
			}
		}

		return assigned;
	}

	/// <summary>
	/// プレハブ配下のどれかのコンポーネントにある名前のフィールドへ値を入れる
	/// </summary>
	static bool SetField(GameObject root, string fieldName, Object value)
	{
		MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
		for (int i = 0; i < behaviours.Length; i++)
		{
			if (behaviours[i] == null)
			{
				continue;
			}
			SerializedObject so = new SerializedObject(behaviours[i]);
			SerializedProperty property = so.FindProperty(fieldName);
			if (property == null)
			{
				continue;
			}
			property.objectReferenceValue = value;
			so.ApplyModifiedPropertiesWithoutUndo();
			return true;
		}
		return false;
	}

	static Object GetField(GameObject root, string fieldName)
	{
		MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
		for (int i = 0; i < behaviours.Length; i++)
		{
			if (behaviours[i] == null)
			{
				continue;
			}
			SerializedObject so = new SerializedObject(behaviours[i]);
			SerializedProperty property = so.FindProperty(fieldName);
			if (property != null)
			{
				return property.objectReferenceValue;
			}
		}
		return null;
	}
}
