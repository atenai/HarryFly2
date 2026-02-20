using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// タイトル
/// </summary>
public class Title : MonoBehaviour
{
	[SerializeField] Button button;

	void Start()
	{
		button.onClick.AddListener(Stage1);
	}

	void Update()
	{

	}

	void Stage1()
	{
		SceneManager.LoadScene("Stage1");
	}
}
