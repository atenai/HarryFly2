using UnityEngine;

/// <summary>
/// 燃料追加
/// </summary>
public class Fuel : MonoBehaviour
{
	[Tooltip("追加燃料")]
	[SerializeField] float value = 50;

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			Destroy(gameObject);
		}
	}
}
