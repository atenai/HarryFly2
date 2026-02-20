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
		//Screen.SetResolution(1920, 1080, true, 60);

		//Cursor.visible = false;
		//Cursor.lockState = CursorLockMode.Locked;
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
