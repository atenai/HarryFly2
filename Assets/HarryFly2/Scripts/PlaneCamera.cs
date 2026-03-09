using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 飛行機カメラ
/// </summary>
public class PlaneCamera : MonoBehaviour
{
	[Tooltip("飛行機のモデル")]
	[SerializeField] PlaneController planeController;

	[Tooltip("UI")]
	[SerializeField] UI ui;

	[Header("カメラに関する変数")]
	[Tooltip("子のメインカメラ")]
	[SerializeField] CinemachineVirtualCamera childMainDashMoveVirtualCamera;

	[Tooltip("カメラ位置のy位置")]
	float verticalOffset = 0.5f;
	[Tooltip("カメラのz位置")]
	float normalDistance = -2.5f;

	void LateUpdate()
	{
		// オフセットは現在の距離を使う
		Vector3 cameraPos = planeController.transform.position + planeController.transform.rotation * new Vector3(0, verticalOffset, normalDistance);

		// カメラ位置をスムーズに移動
		this.transform.position = cameraPos;

		if (ui.ButtonDownFlag == true && 0 < planeController.CurrentFuel)
		{
			//徐々に子カメラをダッシュ時の位置にする
			childMainDashMoveVirtualCamera.Priority = 200;
		}
		else
		{
			//徐々に子カメラを通常時の位置にする
			childMainDashMoveVirtualCamera.Priority = 10;
		}
	}
}
