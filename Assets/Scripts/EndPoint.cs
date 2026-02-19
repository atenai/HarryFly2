using UnityEngine;

/// <summary>
/// ゴール
/// </summary>
public class EndPoint : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.GameClear();
		}
	}
}
