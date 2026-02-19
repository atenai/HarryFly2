using UnityEngine;

/// <summary>
/// 燃料追加
/// </summary>
public class Fuel : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			PlaneController.SingletonInstance.ChangeBrustSlider(0.5f);
			Destroy(gameObject);
		}
	}
}
