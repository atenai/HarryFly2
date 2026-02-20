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

	private float normalDistance = 2.5f;
	private float accelerateDistance = 10f;
	private float verticalOffset = 0.5f;

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
		bool isAccelerating = UI.SingletonInstance.ButtonDownFlag == true && 0 < PlaneController.SingletonInstance.CurrentFuel;
		float targetDistance = isAccelerating ? accelerateDistance : normalDistance;

		// オフセットは現在の距離を使う
		Vector3 offset = new Vector3(0f, verticalOffset, -targetDistance);
		Vector3 cameraPos = PlaneController.SingletonInstance.transform.position + PlaneController.SingletonInstance.transform.rotation * offset;

		// カメラ位置をスムーズに移動
		this.transform.position = cameraPos;
	}
}
