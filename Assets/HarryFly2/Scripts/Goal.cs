using UnityEngine;

/// <summary>
/// ゴール
/// </summary>
public class Goal : MonoBehaviour
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
