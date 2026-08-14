using UnityEngine;

/// <summary>
/// コイン追加
/// </summary>
public class Coin : MonoBehaviour
{
	[Tooltip("追加コイン数")]
	[SerializeField] int value = 1;
	public int Value => value;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") == true)
		{
			Destroy(gameObject);
		}
	}
}
