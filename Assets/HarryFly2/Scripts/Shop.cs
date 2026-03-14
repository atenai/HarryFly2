using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ショップ
/// </summary>
public class Shop : MonoBehaviour
{
	[SerializeField] GameObject Panel_Shop;

	[SerializeField] Button openButton;
	[SerializeField] Button closeButton;
	[SerializeField] Button[] modelButton;

	void Start()
	{
		Panel_Shop.SetActive(false);
		openButton.onClick.AddListener(OnClickShopOpen);
		closeButton.onClick.AddListener(OnClickShopClose);
		modelButton[0].onClick.AddListener(OnClickModel0);
		modelButton[1].onClick.AddListener(OnClickModel1);
	}

	void OnClickShopOpen()
	{
		Panel_Shop.SetActive(true);
		ShopManager.SingletonInstance.IsShopActive = true;
	}

	void OnClickShopClose()
	{
		Panel_Shop.SetActive(false);
		ShopManager.SingletonInstance.IsShopActive = false;
	}

	void OnClickModel0()
	{
		ShopManager.SingletonInstance.PlaneModelNumber = 0;
	}

	void OnClickModel1()
	{
		ShopManager.SingletonInstance.PlaneModelNumber = 1;
	}

	void Update()
	{

	}
}
