using UnityEngine;

public class Timer : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            GameManager.instance.changeTimer(5);
            Destroy(gameObject);
        }
    }
}
