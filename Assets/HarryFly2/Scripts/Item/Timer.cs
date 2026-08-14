using UnityEngine;

/// <summary>
/// 時間追加
/// </summary>
public class Timer : MonoBehaviour
{
	[Tooltip("追加時間")]
	[SerializeField] float value = 5;
	public float Value => value;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") == true)
		{
			Destroy(gameObject);
		}
	}
}
