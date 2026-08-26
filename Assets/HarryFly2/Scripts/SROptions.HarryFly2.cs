using System.ComponentModel;
using UnityEngine;

/// <summary>
/// SRDebugger の Options タブに出すデバッグ項目。
///
/// SROptions は Assets/StompyRobot/SROptions/SROptions.cs で定義された partial class で、
/// アセンブリ定義ファイルの外（Assembly-CSharp）にあるため、
/// ここからゲーム側のクラスをそのまま参照できる。
///
/// 実機ではデバッグパネルを画面の隅の3本指タップなどで開く。
/// 開き方は SRDebugger の Settings（Window > SRDebugger > Settings Window）で変えられる
/// </summary>
public partial class SROptions
{
	// ---- 表示 ---------------------------------------------------------------

	/// <summary>
	/// 画面左上のステージ名表示（StageDebugLabel）の切り替え。
	///
	/// StageDebugLabel は実機で問題の起きたステージを読み取るための機能なので
	/// 既定では出したままにしてあるが、画面を撮りたいときなどに邪魔になる
	/// </summary>
	[Category("表示")]
	[DisplayName("ステージ名を表示")]
	[Sort(0)]
	public bool ShowStageName
	{
		get
		{
			StageDebugLabel label = FindStageDebugLabel();
			return label != null && label.IsVisible;
		}
		set
		{
			StageDebugLabel label = FindStageDebugLabel();
			if (label != null)
			{
				label.SetVisible(value);
			}
			OnPropertyChanged("ShowStageName");
		}
	}

	// ---- ステージ移動 -------------------------------------------------------

	/// <summary>行き先のステージ番号。スライダーで選んでからボタンで飛ぶ</summary>
	int jumpTargetStage = 0;

	/// <summary>
	/// 行き先のステージ番号。
	///
	/// NumberRange はコンパイル時の定数しか渡せないのでスライダーの上限は固定値だが、
	/// 実際の上限は Build Settings のシーン数から取って丸めている。
	/// ステージを増減してもここを直す必要はない
	/// </summary>
	[Category("ステージ")]
	[DisplayName("行き先のステージ番号")]
	[NumberRange(0, 12)]
	[Increment(1)]
	[Sort(0)]
	public int JumpTargetStage
	{
		get { return jumpTargetStage; }
		set
		{
			int last = GetLastStageIndex();
			jumpTargetStage = Mathf.Clamp(value, 0, last);
			OnPropertyChanged("JumpTargetStage");
		}
	}

	[Category("ステージ")]
	[DisplayName("このステージへ移動")]
	[Sort(1)]
	public void JumpToStage()
	{
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager == null)
		{
			Debug.LogWarning("StageManager が見つかりません。");
			return;
		}

		stageManager.JumpToStage(jumpTargetStage);
	}

	[Category("ステージ")]
	[DisplayName("次のステージへ")]
	[Sort(2)]
	public void JumpToNextStage()
	{
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager == null)
		{
			return;
		}

		// 最後まで行ったら最初へ戻す。通常の進行と同じ扱いにする
		int next = stageManager.CurrentStageBuildIndex + 1;
		if (stageManager.LastStageBuildIndex < next)
		{
			next = 0;
		}
		JumpTargetStage = next;
		stageManager.JumpToStage(next);
	}

	[Category("ステージ")]
	[DisplayName("前のステージへ")]
	[Sort(3)]
	public void JumpToPreviousStage()
	{
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager == null)
		{
			return;
		}

		int previous = stageManager.CurrentStageBuildIndex - 1;
		if (previous < 0)
		{
			previous = stageManager.LastStageBuildIndex;
		}
		JumpTargetStage = previous;
		stageManager.JumpToStage(previous);
	}

	[Category("ステージ")]
	[DisplayName("このステージをやり直す")]
	[Sort(4)]
	public void RestartCurrentStage()
	{
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager == null)
		{
			return;
		}

		stageManager.JumpToStage(stageManager.CurrentStageBuildIndex);
	}

	// ---- 内部 ---------------------------------------------------------------

	/// <summary>
	/// ステージ名表示を探す。
	/// StageManager と同じ DontDestroyOnLoad のオブジェクトに付いているので、
	/// シーンをまたいでも見つかる。押したときだけ呼ばれるので毎フレームの負荷にはならない
	/// </summary>
	static StageDebugLabel FindStageDebugLabel()
	{
		return Object.FindObjectOfType<StageDebugLabel>();
	}

	/// <summary>
	/// 最後のステージ番号。StageManager がまだ居ない場面でも呼べるようにしておく
	/// </summary>
	static int GetLastStageIndex()
	{
		StageManager stageManager = StageManager.SingletonInstance;
		if (stageManager != null)
		{
			return stageManager.LastStageBuildIndex;
		}
		return UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1;
	}
}
