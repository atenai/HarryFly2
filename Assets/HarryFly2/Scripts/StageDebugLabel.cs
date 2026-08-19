using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 現在のステージ番号を画面の隅に出すデバッグ表示。
///
/// 実機ではConsoleを見られないので、どのステージで問題が起きたのかを画面から読み取れるようにする。
/// StageManager と同じシングルトンに付けてあるため、ステージが切り替わっても出続ける。
///
/// エディター限定にはしない。実機で確認するための機能なので、
/// #if UNITY_EDITOR で囲むと本来の目的を果たさなくなる
/// </summary>
public class StageDebugLabel : MonoBehaviour
{
	[Tooltip("表示するかどうか。配信ビルドで隠したいときはここを切る")]
	[SerializeField] bool isVisible = true;

	[Tooltip("文字の大きさ。画面の高さに対する比率で決めるので、端末の解像度が変わっても見え方が変わらない")]
	[SerializeField, Range(0.01f, 0.08f)] float fontHeightRatio = 0.025f;

	[Tooltip("画面の端からの余白。画面の高さに対する比率")]
	[SerializeField, Range(0f, 0.1f)] float marginRatio = 0.01f;

	/// <summary>表示に使うスタイル。毎フレーム作り直すとGCを踏むので使い回す</summary>
	GUIStyle labelStyle;

	/// <summary>スタイルを作ったときの文字サイズ。画面が回転して大きさが変わったら作り直す</summary>
	int builtFontSize = -1;

	/// <summary>
	/// 表示の切り替え。実機で見せたくない場面のために外から止められるようにしておく
	/// </summary>
	/// <param name="visible">表示するかどうか</param>
	public void SetVisible(bool visible)
	{
		isVisible = visible;
	}

	void OnGUI()
	{
		if (isVisible == false)
		{
			return;
		}

		int fontSize = Mathf.Max(1, Mathf.RoundToInt(Screen.height * fontHeightRatio));
		if (labelStyle == null || builtFontSize != fontSize)
		{
			labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontSize = fontSize;
			labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.alignment = TextAnchor.UpperLeft;
			builtFontSize = fontSize;
		}

		Scene scene = SceneManager.GetActiveScene();
		string text = "STAGE " + scene.buildIndex + " (" + scene.name + ")";

		float margin = Screen.height * marginRatio;
		Vector2 size = labelStyle.CalcSize(new GUIContent(text));
		Rect rect = new Rect(margin, margin, size.x, size.y);

		// 明るい空の上でも読めるように、影を落としてから本体を描く
		labelStyle.normal.textColor = Color.black;
		GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, labelStyle);

		labelStyle.normal.textColor = Color.yellow;
		GUI.Label(rect, text, labelStyle);
	}
}
