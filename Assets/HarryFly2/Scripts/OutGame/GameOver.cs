using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲームオーバー
/// </summary>
public class GameOver : MonoBehaviour
{
	[SerializeField] Button button;

	void Start()
	{
		button.onClick.AddListener(ReturnTitle);
	}

	void Update()
	{

	}

	void ReturnTitle()
	{
		SceneManager.LoadScene("Title");
	}
}
