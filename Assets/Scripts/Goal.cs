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
			GameManager.SingletonInstance.GameClear();
		}
	}
}
