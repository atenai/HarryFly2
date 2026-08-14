using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
	private static ShopManager singletonInstance = null;
	/// <summary>シングルトンで作成（ゲーム中に１つのみにする）</summary>
	public static ShopManager SingletonInstance => singletonInstance;

	int planeModelNumber = 0;
	public int PlaneModelNumber
	{
		get { return planeModelNumber; }
		set { planeModelNumber = value; }
	}

	[Header("Shop Settings")]
	[Tooltip("各モデルのアンロック価格 (インデックスに対応)。要素数がショップに並ぶ機体数になる")]
	[SerializeField] int[] modelPrices = new int[] { 0, 100, 500, 1500, 3000, 5000 };

	// アンロック状態の配列
	bool[] unlockedModels = null;

	void Start()
	{
		LoadUnlockedStates();
		// デフォルトでモデル0はアンロックされている
		if (unlockedModels == null || unlockedModels.Length == 0)
		{
			unlockedModels = new bool[modelPrices.Length];
			if (unlockedModels.Length > 0) unlockedModels[0] = true;
			SaveUnlockedStates();
		}
	}

	void Awake()
	{
		//staticな変数instanceはメモリ領域は確保されていますが、初回では中身が入っていないので、中身を入れます。
		if (singletonInstance == null)
		{
			singletonInstance = this;//thisというのは自分自身のインスタンスという意味になります。この場合、Playerのインスタンスという意味になります。
			DontDestroyOnLoad(this.gameObject);//シーンを切り替えた時に破棄しない
		}
		else
		{
			Destroy(this.gameObject);//中身がすでに入っていた場合、自身のインスタンスがくっついているゲームオブジェクトを破棄します。
		}
	}

	/// <summary>
	/// 指定モデルがアンロックされているか
	/// </summary>
	public bool IsUnlocked(int index)
	{
		if (unlockedModels == null) return false;
		if (index < 0 || index >= unlockedModels.Length) return false;
		return unlockedModels[index];
	}

	/// <summary>
	/// 指定モデルの価格を返す。範囲外なら-1を返す。
	/// </summary>
	public int GetPrice(int index)
	{
		if (modelPrices == null) return -1;
		if (index < 0 || index >= modelPrices.Length) return -1;
		return modelPrices[index];
	}

	/// <summary>
	/// モデルを選択します。アンロックされていなければコインを消費して購入を試行します。
	/// 成功した場合は選択してtrueを返します。
	/// </summary>
	public bool SelectModel(int index)
	{
		if (index < 0 || index >= modelPrices.Length)
		{
			Debug.LogWarning("SelectModel: invalid index " + index);
			return false;
		}

		if (IsUnlocked(index))
		{
			PlaneModelNumber = index;
			return true;
		}

		int price = modelPrices[index];
		GameManager gm = FindObjectOfType<GameManager>();
		if (gm == null)
		{
			Debug.LogWarning("SelectModel: GameManager not found");
			return false;
		}

		if (gm.SpendCoin(price))
		{
			// 購入成功
			if (unlockedModels == null || unlockedModels.Length != modelPrices.Length)
			{
				unlockedModels = new bool[modelPrices.Length];
			}
			unlockedModels[index] = true;
			SaveUnlockedStates();
			PlaneModelNumber = index;
			Debug.Log("Model " + index + " purchased and selected.");
			return true;
		}
		else
		{
			Debug.Log("Not enough coins to purchase model " + index);
			return false;
		}
	}

	void SaveUnlockedStates()
	{
		if (unlockedModels == null) return;
		ES3.Save("UnlockedModels", unlockedModels);
	}

	void LoadUnlockedStates()
	{
		if (ES3.KeyExists("UnlockedModels"))
		{
			unlockedModels = ES3.Load<bool[]>("UnlockedModels");
		}

		// 機体数を増やすと、古いセーブは要素数が足りない状態で読み込まれる。
		// 元の実装は unlockedModels が null のときに Length を参照して落ちていたので、
		// コピー元の有無を先に判定する
		if (unlockedModels == null || unlockedModels.Length != modelPrices.Length)
		{
			bool[] newArr = new bool[modelPrices.Length];
			int copyCount = unlockedModels != null ? Mathf.Min(newArr.Length, unlockedModels.Length) : 0;
			for (int i = 0; i < copyCount; i++)
			{
				newArr[i] = unlockedModels[i];
			}
			unlockedModels = newArr;
		}

		// 0番は価格0の初期機体なので常に解放しておく
		if (unlockedModels.Length > 0)
		{
			unlockedModels[0] = true;
		}
	}
}
