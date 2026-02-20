using UnityEngine;

/// <summary>
/// 障害物
/// </summary>
public class Obstacle : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.GameOver();
		}
	}
}
