using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 長い音源から必要な部分だけを切り出して、単発の効果音アセットを作る。
///
/// SoundBits の無料版は「1ファイルに複数のテイクを並べたデモ音源」で、
/// 先頭に無音があったり、ボタン音が3連発で入っていたりする。
/// そのまま AudioClip として割り当てると、押すたびに3回鳴ったり、
/// 鳴り始めるまで0.3秒待たされたりする。
///
/// 実行例（-nographics を付けても動く）:
///   Unity.exe -quit -batchmode -projectPath &lt;プロジェクト&gt;
///             -executeMethod AudioSliceTool.ExtractAll -logFile &lt;ログパス&gt;
/// </summary>
public static class AudioSliceTool
{
	/// <summary>ログから結果を拾うための固定マーカー</summary>
	const string Done_Marker = "HF2_SLICE_DONE";

	/// <summary>切り出したものを置く場所</summary>
	const string OutputDirectory = "Assets/HarryFly2/Audio";

	const string SourceDirectory = "Assets/SoundBits_FreeSFX_2020/";

	/// <summary>
	/// 切り出し1件ぶんの指定
	/// </summary>
	class Slice
	{
		public string source;
		public float start;
		public float end;
		public string output;
		/// <summary>頭のプツッを防ぐフェードイン（秒）</summary>
		public float fadeIn = 0.005f;
		/// <summary>途中で切った尻尾のプツッを防ぐフェードアウト（秒）</summary>
		public float fadeOut = 0.05f;
		/// <summary>この音量まで揃える。素材ごとにピークがばらばらなので合わせる</summary>
		public float normalizeTo = 0.9f;
	}

	static readonly Slice[] Slices =
	{
		// 対空砲の発砲。
		// このファイルは1.4秒あたりから延々と盛り上がっていく作りで、
		// 実際に炸裂するのは3.05〜3.65秒（ピーク0.74）。それ以前は助走で、
		// そこを切り出すと正規化で暗騒音だけが持ち上がってしまう。
		// 発砲間隔が0.9秒なので、炸裂の頭から0.6秒ぶんだけ取る
		new Slice { source = "SotD_2013-10-20_(GunShot)", start = 2.980f, end = 3.580f, output = "AntiAirGunFire", fadeOut = 0.15f },

		// 警告音。元は同じビープが1秒間隔で12回並んでいるので1回ぶんだけ取る
		new Slice { source = "CSFX-2_Alarms_15", start = 0.000f, end = 0.790f, output = "Alarm", fadeOut = 0.03f },

		// UIのボタン。3連発の1発目が一番強い（ピーク0.80）
		new Slice { source = "BSL-Buttons_Switches_048", start = 0.140f, end = 0.330f, output = "UIClick", fadeOut = 0.03f },

		// ショップを開く／閉じる。別々のスイッチ音を割り当てて区別できるようにする
		new Slice { source = "BSL-Buttons_Switches_005", start = 5.810f, end = 5.990f, output = "UIOpen", fadeOut = 0.03f },
		new Slice { source = "BSL-Buttons_Switches_005", start = 6.065f, end = 6.185f, output = "UIClose", fadeOut = 0.03f },

		// ゴール。SFっぽい上昇トランジション
		new Slice { source = "Just_Transitions_SciFi-040", start = 0.255f, end = 1.700f, output = "Goal", fadeOut = 0.15f },

		// 弾が脇を通り抜ける音。短く鋭いもの
		new Slice { source = "JustWhoosh3_Swoosh_Rod_Pole_017", start = 0.100f, end = 0.520f, output = "BulletPassBy", fadeOut = 0.10f },

		// 照明弾の打ち上げ
		new Slice { source = "JustWhoosh3_Whoosh_Fire_008", start = 0.150f, end = 0.700f, output = "FlareLaunch", fadeOut = 0.10f },

		// ステージ切り替えのウーッシュ
		new Slice { source = "Just_Whoosh_033", start = 0.540f, end = 1.500f, output = "StageTransition", fadeOut = 0.15f },

		// 時間切れ。アラームとは別のビープを使って、燃料切れと聞き分けられるようにする
		new Slice { source = "CSFX-2_Alarms_15", start = 2.005f, end = 3.000f, output = "TimeUp", fadeOut = 0.15f },

		// 残り時間のカウントダウン。1秒ごとに鳴るので短く乾いた高音にする。
		// 燃料系の警告（Alarm / FuelWarning）と帯域を分けないと聞き分けられない
		new Slice { source = "GlitchFX_02_Percussion_019", start = 0.000f, end = 0.280f, output = "CountdownTick", fadeOut = 0.04f },

		// 燃料が残りわずかになったときの予告。カウントダウンより長めにして区別する
		new Slice { source = "CSFX_short-040", start = 0.000f, end = 0.460f, output = "FuelWarning", fadeOut = 0.06f },

		// 障害物への激突。爆発（1.78秒）の頭に重ねるので短く切る。
		// 長いと爆発と食い合って両方潰れる
		new Slice { source = "JustImpacts-Extension2_Metal_Hit_Crash_200", start = 0.000f, end = 0.550f, output = "ObstacleImpact", fadeOut = 0.12f },

		// 対空砲による撃墜。激突とは別の質感にして死因を聞き分けられるようにする
		new Slice { source = "Just_Impacts_Extension-I_167", start = 0.000f, end = 0.500f, output = "ShotDown", fadeOut = 0.12f },
	};

	public static void ExtractAll()
	{
		Directory.CreateDirectory(OutputDirectory);

		int done = 0;
		for (int i = 0; i < Slices.Length; i++)
		{
			if (Extract(Slices[i]) == true)
			{
				done++;
			}
		}

		AssetDatabase.Refresh();
		ApplyImportSettings();

		Debug.Log(Done_Marker + " extracted=" + done + "/" + Slices.Length + " dir=" + OutputDirectory);
		EditorApplication.Exit(0);
	}

	static bool Extract(Slice slice)
	{
		string sourcePath = SourceDirectory + slice.source + ".wav";
		AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
		if (clip == null)
		{
			Debug.LogError("元の音源が見つかりません: " + sourcePath);
			return false;
		}

		float[] data = new float[clip.samples * clip.channels];
		if (clip.GetData(data, 0) == false)
		{
			Debug.LogError("GetData に失敗しました: " + sourcePath);
			return false;
		}

		int channels = clip.channels;
		int frequency = clip.frequency;
		int startFrame = Mathf.Clamp(Mathf.RoundToInt(slice.start * frequency), 0, clip.samples - 1);
		int endFrame = Mathf.Clamp(Mathf.RoundToInt(slice.end * frequency), startFrame + 1, clip.samples);
		int length = endFrame - startFrame;

		// 2Dで鳴らすものばかりなのでモノラルに落とす。容量とメモリが半分になる
		float[] mono = new float[length];
		for (int i = 0; i < length; i++)
		{
			float sum = 0f;
			for (int c = 0; c < channels; c++)
			{
				sum = sum + data[(startFrame + i) * channels + c];
			}
			mono[i] = sum / channels;
		}

		// 素材ごとにピークがばらばら（0.44〜0.88）なので揃える。
		// 揃えておかないと、インスペクタの音量が同じでも鳴り方が変わってしまう
		float peak = 0f;
		for (int i = 0; i < length; i++)
		{
			float v = Mathf.Abs(mono[i]);
			if (peak < v)
			{
				peak = v;
			}
		}
		if (peak > 0.0001f && slice.normalizeTo > 0f)
		{
			float gain = slice.normalizeTo / peak;
			for (int i = 0; i < length; i++)
			{
				mono[i] = Mathf.Clamp(mono[i] * gain, -1f, 1f);
			}
		}

		// 途中で切った波形をそのまま繋ぐとプツッと鳴るので、両端を落とす
		ApplyFade(mono, frequency, slice.fadeIn, true);
		ApplyFade(mono, frequency, slice.fadeOut, false);

		string outputPath = Path.Combine(OutputDirectory, slice.output + ".wav");
		WriteWav16(outputPath, mono, frequency);

		Debug.Log(slice.output + " <- " + slice.source + " [" + slice.start.ToString("F3") + "-" + slice.end.ToString("F3")
			+ "] " + (length / (float)frequency).ToString("F2") + "s peak=" + peak.ToString("F2"));
		return true;
	}

	/// <summary>
	/// 端の音量を落とす
	/// </summary>
	/// <param name="samples">対象</param>
	/// <param name="frequency">サンプリング周波数</param>
	/// <param name="seconds">フェードの長さ（秒）</param>
	/// <param name="isFadeIn">先頭側ならtrue</param>
	static void ApplyFade(float[] samples, int frequency, float seconds, bool isFadeIn)
	{
		int count = Mathf.Min(samples.Length, Mathf.RoundToInt(seconds * frequency));
		if (count <= 0)
		{
			return;
		}

		for (int i = 0; i < count; i++)
		{
			float t = (float)i / count;
			if (isFadeIn == true)
			{
				samples[i] = samples[i] * t;
			}
			else
			{
				samples[samples.Length - 1 - i] = samples[samples.Length - 1 - i] * t;
			}
		}
	}

	/// <summary>
	/// 16bit PCM のモノラルWAVとして書き出す
	/// </summary>
	static void WriteWav16(string path, float[] samples, int frequency)
	{
		const int Channels = 1;
		const int BitsPerSample = 16;
		int dataSize = samples.Length * Channels * (BitsPerSample / 8);

		using (FileStream stream = new FileStream(path, FileMode.Create))
		using (BinaryWriter writer = new BinaryWriter(stream))
		{
			writer.Write(new char[] { 'R', 'I', 'F', 'F' });
			writer.Write(36 + dataSize);
			writer.Write(new char[] { 'W', 'A', 'V', 'E' });

			writer.Write(new char[] { 'f', 'm', 't', ' ' });
			writer.Write(16);                                      // fmtチャンクの大きさ
			writer.Write((short)1);                                // 1 = PCM
			writer.Write((short)Channels);
			writer.Write(frequency);
			writer.Write(frequency * Channels * (BitsPerSample / 8)); // バイト毎秒
			writer.Write((short)(Channels * (BitsPerSample / 8)));    // ブロック境界
			writer.Write((short)BitsPerSample);

			writer.Write(new char[] { 'd', 'a', 't', 'a' });
			writer.Write(dataSize);
			for (int i = 0; i < samples.Length; i++)
			{
				writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), short.MinValue, short.MaxValue));
			}
		}
	}

	/// <summary>
	/// 切り出したものをモバイル向けの設定にする。
	/// どれも短い2Dの効果音なので、展開済みで持っておくのが一番軽い
	/// </summary>
	static void ApplyImportSettings()
	{
		for (int i = 0; i < Slices.Length; i++)
		{
			string path = Path.Combine(OutputDirectory, Slices[i].output + ".wav").Replace("\\", "/");
			AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
			if (importer == null)
			{
				continue;
			}

			importer.forceToMono = true;
			importer.loadInBackground = false;

			AudioImporterSampleSettings settings = importer.defaultSampleSettings;
			settings.loadType = AudioClipLoadType.DecompressOnLoad;
			settings.compressionFormat = AudioCompressionFormat.PCM;
			importer.defaultSampleSettings = settings;

			importer.SaveAndReimport();
		}
	}
}
