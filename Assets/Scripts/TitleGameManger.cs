using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleGameManger : MonoBehaviour
{
    void Start()
    {
        Screen.SetResolution(1920, 1080, true, 60);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.anyKey)
        {
            SceneManager.LoadScene("PlayScene");
        }
    }

    public void ToPlayScene()
    {
        SceneManager.LoadScene("PlayScene");
    }
}
