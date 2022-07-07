using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fuel : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlaneController.instance.ChangeBrustSlider(.5f);
            Destroy(gameObject);
        }
    }
}
