using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スクリーン設定
/// </summary>
public class ScreenSetting : MonoBehaviour
{
	/// <summary>
	/// 目標フレームレート。
	/// ブーストの燃費（fuelConsumptionPerSecond）と加速ランプ（changeForwordMoveSpeedPerSecond など）は
	/// この値を基準に調整してあるので、変えるとブーストの体感が変わる
	/// </summary>
	const int Target_Frame_Rate = 60;

	void Start()
	{
		// 画面の向きを縦のみに設定
		Screen.orientation = ScreenOrientation.Portrait;

		// VSyncが有効だと targetFrameRate は無視されるので、先に切っておく。
		// Androidでは元々VSyncの設定は参照されないが、エディターでも実機と同じ
		// フレームレートで確認できるようにするために必要
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = Target_Frame_Rate;
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
