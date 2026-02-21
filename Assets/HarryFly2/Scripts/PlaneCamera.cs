using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飛行機カメラ
/// </summary>
public class PlaneCamera : MonoBehaviour
{
	private static PlaneCamera singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static PlaneCamera SingletonInstance => singletonInstance;

	[Tooltip("子のメインカメラ")]
	[SerializeField] GameObject childMainCamera;

	[Tooltip("カメラ位置の縦位置")]
	private float verticalOffset = 0.5f;
	[Tooltip("通常時のカメラ位置")]
	private float normalDistance = -2.5f;
	[Tooltip("加速時のカメラ位置")]
	private float accelerateDistance = -7f;
	[Tooltip("カメラ位置のチェンジスピード")]
	private float cameraChangeSpeed = 3.5f;


	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
		}
		else
		{
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
		}
	}

	void Start()
	{

	}

	void LateUpdate()
	{
		if (UI.SingletonInstance.ButtonDownFlag == true && 0 < PlaneController.SingletonInstance.CurrentFuel)
		{
			//徐々に子カメラを加速時の位置にする
			ChangeChildCameraPos(-cameraChangeSpeed * Time.deltaTime);
		}
		else
		{
			//徐々に子カメラを通常時の位置にする
			ChangeChildCameraPos(cameraChangeSpeed * Time.deltaTime * 2);
		}

		// オフセットは現在の距離を使う
		Vector3 offset = new Vector3(0, verticalOffset, 0);
		Vector3 cameraPos = PlaneController.SingletonInstance.transform.position + PlaneController.SingletonInstance.transform.rotation * offset;

		// カメラ位置をスムーズに移動
		this.transform.position = cameraPos;
	}

	/// <summary>
	/// 加速時に子カメラの距離を変える
	/// </summary>
	/// <param name="value">徐々に変える値</param>
	public void ChangeChildCameraPos(float value)
	{
		childMainCamera.transform.Translate(0, 0, value, Space.World);

		//指定した通常時のカメラの位置より飛行機に近づいた場合
		if (normalDistance <= childMainCamera.transform.localPosition.z)
		{
			//通常時のカメラ位置にする
			childMainCamera.transform.localPosition = new Vector3(0, 0, normalDistance);
		}
		//指定した加速時のカメラの位置より飛行機に遠のいた場合
		if (childMainCamera.transform.localPosition.z <= accelerateDistance)
		{
			//加速時のカメラ位置にする
			childMainCamera.transform.localPosition = new Vector3(0, 0, accelerateDistance);
		}
	}
}
