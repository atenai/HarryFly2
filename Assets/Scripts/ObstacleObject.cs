using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    public GameObject player;
    public ReturenPoint returnController;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            player.GetComponent<PlaneController>().Obstacle();
            returnController.ReturnPoint();
        }
    }
}
