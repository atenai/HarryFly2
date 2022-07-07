using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleGameManger : MonoBehaviour
{
    private void Start()
    {
        Screen.SetResolution(1920, 1080, true,60);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
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
