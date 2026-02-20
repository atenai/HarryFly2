using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スクリーン設定
/// </summary>
public class ScreenSetting : MonoBehaviour
{
	void Start()
	{
		// 画面の向きを縦のみに設定
		Screen.orientation = ScreenOrientation.Portrait;
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
