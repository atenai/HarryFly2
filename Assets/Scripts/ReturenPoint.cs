using UnityEngine;

public class ReturenPoint : MonoBehaviour
{
    public GameObject player;
    Transform returnPoint;
    public int returnPointNumber = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            changNumber();
        }
    }

    void FixedUpdate()
    {
        if (returnPointNumber > 0)
        {
            returnPoint = transform.GetChild(returnPointNumber - 1);
        }

    }

    void changNumber()
    {
        returnPointNumber++;
        transform.GetChild(returnPointNumber - 1).gameObject.GetComponent<MeshRenderer>().enabled = false;
        Destroy(transform.GetChild(returnPointNumber - 1).gameObject.GetComponent<BoxCollider>());
    }

    public void ReturnPoint()
    {
        player.transform.position = returnPoint.position;
    }
}
