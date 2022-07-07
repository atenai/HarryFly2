using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //UI.
    public GameObject gameClearUI;
    public GameObject gameOverUI;
    //self.
    public static GameManager instance;
    //timer.
    public Text timer;
    private float leftTime=30;
    //游戏结束没.
    private bool gameOver = false;

    void Start()
    {
        Screen.SetResolution(1920, 1080, true, 60);
        gameClearUI.SetActive(false);
        gameOverUI.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        instance = this;

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        leftTime -= Time.deltaTime;
        timer.text = "残り時間："+leftTime.ToString("f1");
        if (leftTime <= 0)
        {
            GameOver();
        }

    }

    public void Replay()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("PlayScene");
    }

    //To menu.
    public void ToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("TitleScene");
    }

    public void changeTimer(float value)
    {
        leftTime += value;
    }

    public void GameClear()
    {
        if (gameOver == false)
        {
            gameOver = true;
            gameClearUI.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

    }
    public void GameOver()
    {
        if (gameOver == false)
        {
            gameOver = true;
            gameOverUI.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

    }
}
