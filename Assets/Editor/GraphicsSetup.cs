using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ポストプロセスのプロファイルと、URPアセットのモバイル向け設定をまとめて作り直す。
///
/// VolumeProfile.Add() はコンポーネントをリストに足すだけで、アセットには保存しない。
/// AssetDatabase.AddObjectToAsset() でサブアセットにしないと、
/// エディタを開き直した時点で参照が全部 null に戻る（実際に一度そうなった）。
///
/// 実行例（-nographics を付けても動く）:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod GraphicsSetup.SetupAll -logFile &lt;ログパス&gt;
/// </summary>
public static class GraphicsSetup
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_GFX_DONE";

	const string ProfilePath = "Assets/Settings/PostProcess-Global.asset";
	const string UrpAssetPath = "Assets/Settings/URP-Asset.asset";

	public static void SetupAll()
	{
		RebuildProfile();
		ApplyMobileRenderSettings();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log(Done_Marker);
		EditorApplication.Exit(0);
	}

	/// <summary>
	/// ポストプロセスのプロファイルを作り直す。
	/// 壊れた参照が残っていても直るように、中身を空にしてから積み直す
	/// </summary>
	static void RebuildProfile()
	{
		VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
		if (profile == null)
		{
			profile = ScriptableObject.CreateInstance<VolumeProfile>();
			AssetDatabase.CreateAsset(profile, ProfilePath);
		}

		// 既にサブアセットとしてぶら下がっているものを消してから作り直す
		Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ProfilePath);
		for (int i = 0; i < subAssets.Length; i++)
		{
			if (subAssets[i] is VolumeComponent)
			{
				AssetDatabase.RemoveObjectFromAsset(subAssets[i]);
				Object.DestroyImmediate(subAssets[i], true);
			}
		}
		profile.components.Clear();

		// --- トーンマッピング。素のURPは階調が眠いので入れる。ほぼ無料 ---
		Tonemapping tone = AddComponent<Tonemapping>(profile);
		tone.mode.overrideState = true;
		tone.mode.value = TonemappingMode.Neutral;

		// --- 色調整。これもほぼ無料 ---
		ColorAdjustments color = AddComponent<ColorAdjustments>(profile);
		color.postExposure.overrideState = true; color.postExposure.value = 0.1f;
		color.contrast.overrideState = true;     color.contrast.value = 12f;
		color.saturation.overrideState = true;   color.saturation.value = 8f;

		// --- 周辺減光。縦画面で中央に目線を集める。ほぼ無料 ---
		Vignette vignette = AddComponent<Vignette>(profile);
		vignette.intensity.overrideState = true;  vignette.intensity.value = 0.28f;
		vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.4f;

		// --- Bloom だけは縮小と拡大を繰り返すので実費が掛かる。
		//     反復回数を落とし、高品質フィルタは切って最小構成にする ---
		Bloom bloom = AddComponent<Bloom>(profile);
		bloom.threshold.overrideState = true;            bloom.threshold.value = 1.1f;
		bloom.intensity.overrideState = true;            bloom.intensity.value = 0.5f;
		bloom.scatter.overrideState = true;              bloom.scatter.value = 0.6f;
		bloom.highQualityFiltering.overrideState = true; bloom.highQualityFiltering.value = false;
		bloom.downscale.overrideState = true;            bloom.downscale.value = BloomDownscaleMode.Half;
		bloom.maxIterations.overrideState = true;        bloom.maxIterations.value = 3;

		EditorUtility.SetDirty(profile);
		AssetDatabase.SaveAssets();

		// 保存し直したものを読んで、参照が生きているか確かめる
		VolumeProfile check = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
		int alive = 0;
		for (int i = 0; i < check.components.Count; i++)
		{
			if (check.components[i] != null)
			{
				alive++;
			}
		}
		Debug.Log("profile components=" + check.components.Count + " alive=" + alive);
	}

	/// <summary>
	/// プロファイルにコンポーネントを足し、アセットにも保存する
	/// </summary>
	static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
	{
		T component = profile.Add<T>(true);
		component.active = true;
		// これを忘れると、開き直したときに参照が null になる
		AssetDatabase.AddObjectToAsset(component, profile);
		return component;
	}

	/// <summary>
	/// URPアセットをモバイル向けに落とす。
	///
	/// 影は丸ごと切る。機体は毎秒300〜1500で飛ぶので落ちた影はほとんど認識できないのに、
	/// シャドウマップを描くためのジオメトリパスが1回まるごと増える。
	/// MSAAは4xだとタイルメモリを4倍使うので2xに戻す
	/// </summary>
	static void ApplyMobileRenderSettings()
	{
		UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
		if (urp == null)
		{
			Debug.LogError("URP asset not found: " + UrpAssetPath);
			return;
		}

		SerializedObject so = new SerializedObject(urp);
		SetInt(so, "m_MSAA", 2);
		SetBool(so, "m_MainLightShadowsSupported", false);
		SetBool(so, "m_SoftShadowsSupported", false);
		SetFloat(so, "m_ShadowDistance", 50f);
		SetInt(so, "m_ColorGradingMode", 0);          // HDRグレーディングは重い。LDRで足りる
		SetInt(so, "m_StoreActionsOptimization", 1);  // タイルGPUで不要な書き戻しを省く
		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(urp);
		AssetDatabase.SaveAssets();

		SerializedObject check = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath));
		Debug.Log("URP: msaa=" + check.FindProperty("m_MSAA").intValue
			+ " mainShadows=" + check.FindProperty("m_MainLightShadowsSupported").boolValue
			+ " softShadows=" + check.FindProperty("m_SoftShadowsSupported").boolValue
			+ " shadowDist=" + check.FindProperty("m_ShadowDistance").floatValue
			+ " grading=" + check.FindProperty("m_ColorGradingMode").intValue
			+ " storeActions=" + check.FindProperty("m_StoreActionsOptimization").intValue
			+ " hdr=" + check.FindProperty("m_SupportsHDR").boolValue);
	}

	static void SetInt(SerializedObject so, string name, int value)
	{
		SerializedProperty p = so.FindProperty(name);
		if (p == null) { Debug.LogWarning("missing property: " + name); return; }
		p.intValue = value;
	}

	static void SetBool(SerializedObject so, string name, bool value)
	{
		SerializedProperty p = so.FindProperty(name);
		if (p == null) { Debug.LogWarning("missing property: " + name); return; }
		p.boolValue = value;
	}

	static void SetFloat(SerializedObject so, string name, float value)
	{
		SerializedProperty p = so.FindProperty(name);
		if (p == null) { Debug.LogWarning("missing property: " + name); return; }
		p.floatValue = value;
	}
}
