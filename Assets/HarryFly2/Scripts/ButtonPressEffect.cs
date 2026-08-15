using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ボタンを押したときに少し縮ませて、押した手応えを出す。
/// Button の Color Tint だけだと、指で隠れる位置のボタンでは変化がほとんど見えないため、
/// 大きさも一緒に変える。
///
/// 押している間ずっと縮んだままになり、離すと少し跳ねて戻る。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[Tooltip("押し込んだときの大きさの倍率")]
	[SerializeField] float pressedScale = 0.92f;

	[Tooltip("押し込むまでの時間（秒）。長くすると反応が鈍く感じる")]
	[SerializeField] float pressDuration = 0.06f;

	[Tooltip("離してから元の大きさへ戻るまでの時間（秒）")]
	[SerializeField] float releaseDuration = 0.18f;

	RectTransform cachedRect;

	/// <summary>押せない状態のときは反応させないための参照。Button 以外に付けても動くよう null を許容する</summary>
	Selectable cachedSelectable;

	/// <summary>元の大きさ。1倍とは限らないので、実行時の値を控えておく</summary>
	Vector3 defaultScale = Vector3.one;

	void Awake()
	{
		cachedRect = (RectTransform)this.transform;
		cachedSelectable = GetComponent<Selectable>();
		defaultScale = cachedRect.localScale;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (IsInteractable() == false)
		{
			return;
		}

		PlayScale(defaultScale * pressedScale, pressDuration, Ease.OutQuad);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		// 指をボタンの外へずらして離したときもここに来るので、押しっぱなしの見た目で残らない。
		// 押せない状態になっていても、縮んだままにしないよう必ず戻す
		PlayScale(defaultScale, releaseDuration, Ease.OutBack);
	}

	bool IsInteractable()
	{
		return cachedSelectable == null || cachedSelectable.IsInteractable();
	}

	void PlayScale(Vector3 targetScale, float duration, Ease ease)
	{
		cachedRect.DOKill();
		// 広告表示などで timeScale が 0 になっても止まらないように SetUpdate(true) にする
		cachedRect.DOScale(targetScale, duration).SetEase(ease).SetUpdate(true);
	}

	void OnDisable()
	{
		// 押した直後に非表示になるボタン（スタート、ショップを開く）があるので、
		// ここで戻さないと次に出したとき縮んだままになる
		cachedRect.DOKill();
		cachedRect.localScale = defaultScale;
	}
}
