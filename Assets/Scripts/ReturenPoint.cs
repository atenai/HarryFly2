using UnityEngine;

/// <summary>
/// チェックポイント
/// </summary>
public class ReturenPoint : MonoBehaviour
{
	Transform returnPoint;
	public int returnPointNumber = 0;

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			ChangNumber();
		}
	}

	void FixedUpdate()
	{
		if (0 < returnPointNumber)
		{
			returnPoint = transform.GetChild(returnPointNumber - 1);
		}

	}

	void ChangNumber()
	{
		returnPointNumber++;
		transform.GetChild(returnPointNumber - 1).gameObject.GetComponent<MeshRenderer>().enabled = false;
		Destroy(transform.GetChild(returnPointNumber - 1).gameObject.GetComponent<BoxCollider>());
	}

	public void ReturnPoint()
	{
		PlaneController.SingletonInstance.transform.position = returnPoint.position;
	}
}
