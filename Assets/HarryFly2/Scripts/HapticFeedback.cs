using UnityEngine;

/// <summary>
/// 端末を振動させる（Android実機のみ動作。エディタとその他のプラットフォームでは何もしない）。
///
/// 単発の振動しか持たない。以前はブースト中に鳴らし続ける継続振動を持っていたが、
/// 鳴っている最中に単発を割り込ませてもアイテム取得の振動を感じ取れなかったため、
/// 継続振動そのものを廃止した。
/// </summary>
public static class HapticFeedback
{
	/// <summary> 振動の強さ（5段階） </summary>
	public enum Strength
	{
		/// <summary> 1段階目：極弱 </summary>
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

	/// <summary> 振動のオン/オフ（設定画面から切り替える想定） </summary>
	public static bool IsEnabled { get; set; } = true;

	/// <summary> 単発振動が連続しすぎないようにする最小間隔（秒） </summary>
	const float Min_Interval = 0.05f;

	/// <summary> 間引きの対象にする最大の強さ（これより強いものは必ず振動させる） </summary>
	const Strength Max_Throttled_Strength = Strength.Medium;

	/// <summary> 最後に振動させた時刻 </summary>
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

#if UNITY_ANDROID && !UNITY_EDITOR

	/// <summary> VibrationEffect が使えるようになった Android 8.0 のAPIレベル </summary>
	const int Sdk_Version_Oreo = 26;

	/// <summary> 5段階の振動時間（ミリ秒）。Strength の並び順と対応 </summary>
	static readonly long[] Durations = { 15, 30, 45, 60, 85 };

	/// <summary> 5段階の振動の強さ（1〜255）。Strength の並び順と対応 </summary>
	static readonly int[] Amplitudes = { 90, 160, 200, 230, 255 };

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
	/// 端末が強さの指定に対応していなければ既定値に落とす
	/// </summary>
	/// <param name="amplitude">指定したい強さ（1〜255）</param>
	static int ToAmplitude(int amplitude)
	{
		if (hasAmplitudeControl == false)
		{
			return Default_Amplitude;
		}

		return amplitude;
	}

#endif
}
