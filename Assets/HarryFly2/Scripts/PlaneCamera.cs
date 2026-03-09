using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 飛行機カメラ
/// </summary>
public class PlaneCamera : MonoBehaviour
{
	private static PlaneCamera singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static PlaneCamera SingletonInstance => singletonInstance;

	[Tooltip("子のメインカメラ")]
	[SerializeField] CinemachineVirtualCamera childMainDashMoveVirtualCamera;

	[Tooltip("カメラ位置のy位置")]
	float verticalOffset = 0.5f;
	[Tooltip("カメラのz位置")]
	float normalDistance = -2.5f;

	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
			DontDestroyOnLoad(this.gameObject);//シーンを切り替えた時に破棄しない
		}
		else
		{
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
		}
	}

	void LateUpdate()
	{
		// オフセットは現在の距離を使う
		Vector3 cameraPos = PlaneController.SingletonInstance.transform.position + PlaneController.SingletonInstance.transform.rotation * new Vector3(0, verticalOffset, normalDistance);

		// カメラ位置をスムーズに移動
		this.transform.position = cameraPos;

		if (UI.SingletonInstance.ButtonDownFlag == true && 0 < PlaneController.SingletonInstance.CurrentFuel)
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
