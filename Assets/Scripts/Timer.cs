using UnityEngine;

/// <summary>
/// 時間追加
/// </summary>
public class Timer : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.AddTimer(5);
			Destroy(gameObject);
		}
	}
}
