using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    public GameObject player;
    public ReturenPoint returnController;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            player.GetComponent<PlaneController>().Obstacle();
            returnController.ReturnPoint();
        }
    }
}
