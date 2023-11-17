using UnityEngine;

public class Fuel : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlaneController.instance.ChangeBrustSlider(.5f);
            Destroy(gameObject);
        }
    }
}
