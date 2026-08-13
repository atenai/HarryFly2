using UnityEngine;

/// <summary>
/// 端末を振動させる（Android実機のみ動作。エディタとその他のプラットフォームでは何もしない）
/// </summary>
public static class HapticFeedback
{
	/// <summary> 振動の強さ（5段階） </summary>
	public enum Strength
	{
		/// <summary> 1段階目：極弱（ブースト中の継続振動） </summary>
		VeryLight,
		/// <summary> 2段階目：弱（コイン取得） </summary>
		Light,
		/// <summary> 3段階目：中（燃料・時間の取得） </summary>
		Medium,
		/// <summary> 4段階目：強（ゴール） </summary>
		Heavy,
		/// <summary> 5段階目：極強（障害物への衝突） </summary>
		VeryHeavy,
	}

	static bool isEnabled = true;

	/// <summary> 振動のオン/オフ（設定画面から切り替える想定） </summary>
	public static bool IsEnabled
	{
		get => isEnabled;
		set
		{
			isEnabled = value;
			if (isEnabled == false)
			{
				// 継続振動中にオフにされたら止める
				StopContinuous();
			}
		}
	}

	/// <summary> 単発振動が連続しすぎないようにする最小間隔（秒） </summary>
	const float Min_Interval = 0.05f;

	/// <summary> 間引きの対象にする最大の強さ（これより強いものは必ず振動させる） </summary>
	const Strength Max_Throttled_Strength = Strength.Medium;

	/// <summary> 最後に単発振動させた時刻 </summary>
	static float lastPlayedTime = -1f;

	/// <summary>
	/// 単発で振動させる
	/// </summary>
	/// <param name="strength">振動の強さ</param>
	public static void Play(Strength strength)
	{
		if (IsEnabled == false)
		{
			return;
		}

		// アイテムが連続で取れたときに振動が重ならないようにする。
		// ただし衝突やゴールのような強い振動は取りこぼしたくないので間引かない
		if (strength <= Max_Throttled_Strength)
		{
			if (Time.unscaledTime - lastPlayedTime < Min_Interval)
			{
				return;
			}
		}
		lastPlayedTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
		PlayOnAndroid(strength);
#endif
	}

	/// <summary>
	/// 振動を鳴らし続ける（ブースト中など）。
	/// 毎フレーム呼んでよい（既に同じ強さで鳴っていれば何もしない）
	/// </summary>
	/// <param name="strength">振動の強さ</param>
	public static void StartContinuous(Strength strength)
	{
		if (IsEnabled == false)
		{
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		StartContinuousOnAndroid(strength);
#endif
	}

	/// <summary>
	/// 鳴らし続けている振動を止める。
	/// 毎フレーム呼んでよい（鳴っていなければ何もしない）
	/// </summary>
	public static void StopContinuous()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		StopContinuousOnAndroid();
#endif
	}

	/// <summary>
	/// アプリが中断・終了したときに継続振動を止める。
	/// 止め忘れると端末が鳴りっぱなしになるための保険
	/// </summary>
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void RegisterLifecycleEvents()
	{
		Application.focusChanged += OnFocusChanged;
		Application.quitting += StopContinuous;
	}

	static void OnFocusChanged(bool hasFocus)
	{
		if (hasFocus == false)
		{
			StopContinuous();
		}
	}

#if UNITY_ANDROID && !UNITY_EDITOR

	/// <summary> VibrationEffect が使えるようになった Android 8.0 のAPIレベル </summary>
	const int Sdk_Version_Oreo = 26;

	/// <summary> 5段階の振動時間（ミリ秒）。Strength の並び順と対応 </summary>
	static readonly long[] Durations = { 10, 20, 35, 55, 80 };

	/// <summary> 5段階の振動の強さ（1〜255）。Strength の並び順と対応 </summary>
	static readonly int[] Amplitudes = { 50, 90, 130, 190, 255 };

	/// <summary> 継続振動の1周期の長さ（ミリ秒）。これを繰り返して鳴らし続ける </summary>
	const long Continuous_Segment = 400;

	/// <summary> 継続振動中に単発振動をはさむときの無振動の間（ミリ秒） </summary>
	const long Continuous_Gap = 40;

	/// <summary> 端末側で強さを指定できないときに使う既定値（VibrationEffect.DEFAULT_AMPLITUDE） </summary>
	const int Default_Amplitude = -1;

	/// <summary> android.os.Vibrator </summary>
	static AndroidJavaObject vibrator;
	/// <summary> android.os.VibrationEffect </summary>
	static AndroidJavaClass vibrationEffectClass;

	static bool isInitialized;
	/// <summary> この端末で振動が使えるか </summary>
	static bool isAvailable;
	/// <summary> VibrationEffect（Android 8.0以降）が使えるか </summary>
	static bool canUseVibrationEffect;
	/// <summary> 振動の強さを指定できる端末か </summary>
	static bool hasAmplitudeControl;

	/// <summary> 継続振動を鳴らしているか </summary>
	static bool isContinuousPlaying;
	/// <summary> 鳴らしている継続振動の強さ </summary>
	static Strength continuousStrength;

	/// <summary>
	/// Vibrator を取得する（初回のみ）
	/// </summary>
	static void Initialize()
	{
		if (isInitialized == true)
		{
			return;
		}
		isInitialized = true;

		try
		{
			using (AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"))
			{
				// Context.VIBRATOR_SERVICE
				vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
			}

			// 振動モーターを持たない端末（一部のタブレットなど）では何もしない
			if (vibrator == null || vibrator.Call<bool>("hasVibrator") == false)
			{
				return;
			}

			int sdkInt = 0;
			using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
			{
				sdkInt = versionClass.GetStatic<int>("SDK_INT");
			}

			if (Sdk_Version_Oreo <= sdkInt)
			{
				canUseVibrationEffect = true;
				vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
				hasAmplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
			}

			isAvailable = true;
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("振動の初期化に失敗した：" + e.Message);
			isAvailable = false;
		}
	}

	/// <summary>
	/// 単発で振動させる
	/// </summary>
	/// <param name="strength">振動の強さ</param>
	static void PlayOnAndroid(Strength strength)
	{
		Initialize();

		if (isAvailable == false)
		{
			return;
		}

		try
		{
			// 継続振動中に単発を鳴らすと継続側が上書きされて止まってしまうので、
			// 「単発 → 少し空ける → 継続振動に戻る」という一本の波形として鳴らし直す
			if (isContinuousPlaying == true)
			{
				VibrateContinuous(true, strength);
				return;
			}

			if (canUseVibrationEffect == true)
			{
				int amplitude = ToAmplitude(Amplitudes[(int)strength]);
				using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", Durations[(int)strength], amplitude))
				{
					vibrator.Call("vibrate", effect);
				}
			}
			else
			{
				// Android 7.1以下は強さを指定できない
				vibrator.Call("vibrate", Durations[(int)strength]);
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("振動に失敗した：" + e.Message);
			// 失敗し続けても意味がないので以降は振動させない
			isAvailable = false;
		}
	}

	/// <summary>
	/// 継続振動を開始する
	/// </summary>
	/// <param name="strength">振動の強さ</param>
	static void StartContinuousOnAndroid(Strength strength)
	{
		Initialize();

		if (isAvailable == false)
		{
			return;
		}

		// 毎フレーム呼ばれるので、既に同じ強さで鳴っていれば鳴らし直さない
		if (isContinuousPlaying == true && continuousStrength == strength)
		{
			return;
		}

		continuousStrength = strength;
		isContinuousPlaying = true;

		try
		{
			VibrateContinuous(false, Strength.VeryLight);
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("継続振動に失敗した：" + e.Message);
			isContinuousPlaying = false;
			isAvailable = false;
		}
	}

	/// <summary>
	/// 継続振動を止める
	/// </summary>
	static void StopContinuousOnAndroid()
	{
		if (isContinuousPlaying == false)
		{
			return;
		}
		isContinuousPlaying = false;

		if (isAvailable == false)
		{
			return;
		}

		try
		{
			vibrator.Call("cancel");
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("振動の停止に失敗した：" + e.Message);
			isAvailable = false;
		}
	}

	/// <summary>
	/// 繰り返し振動を鳴らす
	/// </summary>
	/// <param name="hasOneShot">先頭に単発の振動をはさむか</param>
	/// <param name="oneShotStrength">はさむ単発振動の強さ</param>
	static void VibrateContinuous(bool hasOneShot, Strength oneShotStrength)
	{
		int continuousAmplitude = ToAmplitude(Amplitudes[(int)continuousStrength]);

		if (canUseVibrationEffect == true)
		{
			long[] timings;
			int[] amplitudes;
			int repeatIndex;

			if (hasOneShot == true)
			{
				// 単発 → 無振動 → 継続。3つ目から先を繰り返す
				timings = new long[] { Durations[(int)oneShotStrength], Continuous_Gap, Continuous_Segment };
				amplitudes = new int[] { ToAmplitude(Amplitudes[(int)oneShotStrength]), 0, continuousAmplitude };
				repeatIndex = 2;
			}
			else
			{
				timings = new long[] { Continuous_Segment };
				amplitudes = new int[] { continuousAmplitude };
				repeatIndex = 0;
			}

			using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, repeatIndex))
			{
				vibrator.Call("vibrate", effect);
			}
		}
		else
		{
			// Android 7.1以下は強さを指定できないので、時間のパターンだけで表現する。
			// パターンは「無振動の長さ, 振動の長さ, ...」の交互指定
			long[] pattern;
			int repeatIndex;

			if (hasOneShot == true)
			{
				pattern = new long[] { 0, Durations[(int)oneShotStrength], Continuous_Gap, Continuous_Segment };
				repeatIndex = 2;
			}
			else
			{
				pattern = new long[] { 0, Continuous_Segment };
				repeatIndex = 0;
			}

			vibrator.Call("vibrate", pattern, repeatIndex);
		}
	}

	/// <summary>
	/// 端末が強さの指定に対応していなければ既定値に落とす
	/// </summary>
	/// <param name="amplitude">指定したい強さ（1〜255）</param>
	static int ToAmplitude(int amplitude)
	{
		// 0 は「振動させない区間」なのでそのまま返す
		if (amplitude <= 0)
		{
			return 0;
		}

		if (hasAmplitudeControl == false)
		{
			return Default_Amplitude;
		}

		return amplitude;
	}

#endif
}
