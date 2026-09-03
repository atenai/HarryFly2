using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 電波障害の演出。
///
/// 残り時間が減るほど画面にノイズを走らせ、時間切れで SIGNAL LOST を出す。
/// この機体は遠隔操縦の UAV なので、時間切れを「電波が届かなくなった」と見立てている。
/// 数字が0になるより、映像が乱れて切れるほうが何が起きたか伝わる。
///
/// Canvas のプレハブには手を入れず、実行時に組み立てる。
/// プレハブに置くと全13ステージのシーンに差分が出るうえ、
/// この演出は画面全体を覆うだけでレイアウトの調整が要らない
/// </summary>
public class SignalNoise : MonoBehaviour
{
	/// <summary>
	/// 用意しておくノイズ画像の枚数。
	/// 毎フレーム作り直すと画素数ぶんの書き込みが走るので、先に作って切り替える
	/// </summary>
	const int Frame_Count = 8;

	/// <summary>
	/// ノイズ画像の一辺。
	/// 画面いっぱいに引き伸ばして使うため、小さくても粗さは目立たない
	/// </summary>
	const int Texture_Size = 128;

	/// <summary>1秒あたりに切り替える枚数。速すぎるとちらつきが不快になる</summary>
	const float Frames_Per_Second = 18f;

	/// <summary>画面に対して何回繰り返すか。1より大きくすると粒が細かくなる</summary>
	const float Tiling = 3f;

	/// <summary>時間切れ前のノイズの最大の濃さ。ここを濃くしすぎると操作の邪魔になる</summary>
	const float Approach_Max_Alpha = 0.38f;

	/// <summary>通信不能になったときの濃さ</summary>
	const float Lost_Alpha = 0.85f;

	/// <summary>切り替えて使うノイズ画像</summary>
	Texture2D[] frames;

	/// <summary>画面全体を覆うノイズ</summary>
	RawImage noiseImage;

	/// <summary>ときどき横に走る帯。ノイズだけだと砂嵐にしか見えない</summary>
	RawImage glitchBand;

	/// <summary>SIGNAL LOST の文字</summary>
	TextMeshProUGUI lostText;

	/// <summary>いまのノイズの強さ（0〜1）</summary>
	float intensity = 0f;

	/// <summary>通信不能の状態かどうか</summary>
	bool isLost = false;

	/// <summary>
	/// Canvas の下に演出を組み立てる
	/// </summary>
	/// <param name="canvas">親にする Canvas</param>
	/// <param name="font">SIGNAL LOST に使う書体。HUD と揃えるため呼び出し側から渡す</param>
	public static SignalNoise Create(Transform canvas, TMP_FontAsset font)
	{
		if (canvas == null)
		{
			return null;
		}

		var root = new GameObject("SignalNoise", typeof(RectTransform));
		root.transform.SetParent(canvas, false);
		// 一番手前に出す。HUD の下に潜ると効果が半減する
		root.transform.SetAsLastSibling();

		var signal = root.AddComponent<SignalNoise>();
		signal.Build(font);
		return signal;
	}

	void Build(TMP_FontAsset font)
	{
		Stretch(this.GetComponent<RectTransform>());

		frames = new Texture2D[Frame_Count];
		for (int i = 0; i < Frame_Count; i++)
		{
			frames[i] = CreateNoiseTexture();
		}

		noiseImage = CreateImage("Noise", this.transform);
		Stretch(noiseImage.rectTransform);
		noiseImage.texture = frames[0];

		glitchBand = CreateImage("GlitchBand", this.transform);
		glitchBand.texture = frames[0];
		// 横いっぱいの細い帯。高さは走らせるときに決める
		var bandRect = glitchBand.rectTransform;
		bandRect.anchorMin = new Vector2(0f, 0.5f);
		bandRect.anchorMax = new Vector2(1f, 0.5f);
		bandRect.offsetMin = new Vector2(0f, -20f);
		bandRect.offsetMax = new Vector2(0f, 20f);

		lostText = CreateLostText(font);

		SetIntensity(0f);
	}

	/// <summary>
	/// 砂嵐の絵を1枚作る。
	/// 白黒をはっきり分けたほうが、薄く重ねたときでも「乱れている」と分かる
	/// </summary>
	static Texture2D CreateNoiseTexture()
	{
		var texture = new Texture2D(Texture_Size, Texture_Size, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Repeat;
		texture.filterMode = FilterMode.Point;

		var pixels = new Color32[Texture_Size * Texture_Size];
		for (int i = 0; i < pixels.Length; i++)
		{
			byte value = Random.value < 0.5f ? (byte)20 : (byte)235;
			byte alpha = (byte)Random.Range(60, 256);
			pixels[i] = new Color32(value, value, value, alpha);
		}
		texture.SetPixels32(pixels);
		texture.Apply();
		return texture;
	}

	static RawImage CreateImage(string name, Transform parent)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		var image = go.AddComponent<RawImage>();
		// 演出が入力を吸うと、加速ボタンが押せなくなる
		image.raycastTarget = false;
		return image;
	}

	TextMeshProUGUI CreateLostText(TMP_FontAsset font)
	{
		var go = new GameObject("LostText", typeof(RectTransform));
		go.transform.SetParent(this.transform, false);

		var text = go.AddComponent<TextMeshProUGUI>();
		if (font != null)
		{
			text.font = font;
		}
		text.text = "SIGNAL LOST";
		text.alignment = TextAlignmentOptions.Center;
		text.enableAutoSizing = true;
		text.fontSizeMin = 20f;
		text.fontSizeMax = 160f;
		text.color = new Color(1f, 0.25f, 0.2f, 1f);
		text.raycastTarget = false;

		var rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.1f, 0.4f);
		rect.anchorMax = new Vector2(0.9f, 0.6f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		go.SetActive(false);
		return text;
	}

	/// <summary>画面いっぱいに広げる</summary>
	static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	/// <summary>
	/// ノイズの強さを決める。
	/// 残り時間から呼ばれる
	/// </summary>
	/// <param name="value">0で消え、1で最大</param>
	public void SetIntensity(float value)
	{
		intensity = Mathf.Clamp01(value);
	}

	/// <summary>
	/// 通信不能にする。時間切れの瞬間に呼ぶ
	/// </summary>
	public void ShowLost()
	{
		isLost = true;
		if (lostText != null)
		{
			lostText.gameObject.SetActive(true);
		}
	}

	void Update()
	{
		if (noiseImage == null)
		{
			return;
		}

		// ステージの切り替えが始まったら消す。
		//
		// 旧ステージは UnloadSceneAsync で消えるので、切り替えを始めても
		// しばらくは残っていて Update も回り続ける。
		// そのままだと次のステージのフェードにノイズが重なって見えてしまう
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager != null && stageManager.IsSceneSwitched == true)
		{
			Hide();
			return;
		}

		float alpha = isLost == true ? Lost_Alpha : intensity * Approach_Max_Alpha;
		if (alpha <= 0.001f)
		{
			// 完全に消えているときは描画も止める。常時 RawImage を重ねると無駄に塗る
			Hide();
			return;
		}

		noiseImage.enabled = true;

		// 絵を切り替えて動かす。時間切れ後も動かしたいので unscaledTime を使う
		int index = (int)(Time.unscaledTime * Frames_Per_Second) % frames.Length;
		noiseImage.texture = frames[index];
		// 同じ絵が同じ位置に出ると模様に見えるので、毎回ずらす
		noiseImage.uvRect = new Rect(Random.value, Random.value, Tiling, Tiling);
		noiseImage.color = new Color(1f, 1f, 1f, alpha);

		UpdateGlitchBand(index, alpha);

		if (isLost == true && lostText != null)
		{
			// 明滅させる。止まった文字より「切れている」感じが出る
			float pulse = Mathf.PingPong(Time.unscaledTime * 6f, 1f);
			Color c = lostText.color;
			c.a = Mathf.Lerp(0.35f, 1f, pulse);
			lostText.color = c;
		}
	}

	/// <summary>
	/// 描画をまとめて止める。
	/// 文字も一緒に消さないと、ノイズだけ消えて SIGNAL LOST が残る
	/// </summary>
	void Hide()
	{
		if (noiseImage != null && noiseImage.enabled == true)
		{
			noiseImage.enabled = false;
		}
		if (glitchBand != null && glitchBand.enabled == true)
		{
			glitchBand.enabled = false;
		}
		if (lostText != null && lostText.gameObject.activeSelf == true)
		{
			lostText.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// 横に走る帯を動かす。
	/// 強さが低いうちはほとんど出さず、切れる直前に頻度を上げる
	/// </summary>
	/// <param name="index">いま使っているノイズ画像の番号</param>
	/// <param name="alpha">ノイズ全体の濃さ</param>
	void UpdateGlitchBand(int index, float alpha)
	{
		if (glitchBand == null)
		{
			return;
		}

		float chance = isLost == true ? 0.5f : intensity * 0.25f;
		if (Random.value > chance)
		{
			glitchBand.enabled = false;
			return;
		}

		glitchBand.enabled = true;
		glitchBand.texture = frames[(index + 3) % frames.Length];
		glitchBand.uvRect = new Rect(Random.value, Random.value, Tiling * 2f, 1f);
		glitchBand.color = new Color(1f, 1f, 1f, Mathf.Min(1f, alpha * 1.6f));

		var rect = glitchBand.rectTransform;
		float height = Random.Range(8f, 46f);
		float center = Random.Range(-0.45f, 0.45f);
		rect.anchorMin = new Vector2(0f, 0.5f + center);
		rect.anchorMax = new Vector2(1f, 0.5f + center);
		rect.offsetMin = new Vector2(0f, -height * 0.5f);
		rect.offsetMax = new Vector2(0f, height * 0.5f);
	}
}
