using UnityEngine;

/// <summary>
/// コイン追加
/// </summary>
public class Coin : MonoBehaviour
{
	[Tooltip("追加コイン数")]
	[SerializeField] int value = 1;

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GameManager.SingletonInstance.AddCoin(value);
			Destroy(gameObject);
		}
	}
}
