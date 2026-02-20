using UnityEngine;

/// <summary>
/// 時間追加
/// </summary>
public class Timer : MonoBehaviour
{
	[Tooltip("追加時間")]
	[SerializeField] float value = 5;

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.AddTimer(value);
			Destroy(gameObject);
		}
	}
}
