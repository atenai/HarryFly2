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
			// シーンを切り替える
			StageManager.SingletonInstance.IsTriggered = true;
		}
	}
}
