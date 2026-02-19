using UnityEngine;

/// <summary>
/// 障害物
/// </summary>
public class ObstacleObject : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.GameOver();
		}
	}
}
