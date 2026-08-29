using UnityEngine;

/// <summary>
/// 対空機関砲。砲身が向いている方向へ、定期的に弾を撃つ。
///
/// 以前は機体を追尾して撃っていた。
/// 砲の位置も分からず、弾も見えず、当たった理由も分からないため、
/// プレイヤーからは「突然わけもなく撃墜される」だけの存在になっていた。
///
/// ここでは狙いを一切付けない。決まった向きへ弾幕を張るだけにしてある。
/// 弾の通る線が毎回同じなので、手前から見て「どこが危ないか」を判断して避けられる。
///
/// 撃つ向きは砲身（barrel）の向きそのもの。
/// シーンで砲を回せば、回した向きへそのまま撃つ
/// </summary>
public class AntiAirGun : MonoBehaviour
{
	[Header("撃つ向き")]
	/// <summary>
	/// 撃つ向きを決める砲身。
	/// この向きへそのまま撃つので、シーンビューで砲身が向いている先が弾の通る線になる
	/// </summary>
	[Tooltip("撃つ向きを決める砲身。この向きへそのまま撃つ。未設定ならこのオブジェクト自身の向き")]
	[SerializeField] Transform barrel;

	[Tooltip("弾を出す位置。未設定なら砲身の位置から出す")]
	[SerializeField] Transform muzzle;

	[Header("弾")]
	[Tooltip("弾のプレハブ")]
	[SerializeField] GameObject bulletPrefab;

	[Tooltip("発砲時に砲口へ出す発光エフェクト")]
	[SerializeField] GameObject muzzleFlashPrefab;

	[Tooltip("発光エフェクトの大きさ。砲の大きさに合わせて調整する")]
	[SerializeField] float muzzleFlashScale = 1f;

	[Header("撃ち方")]
	[Tooltip("1回の斉射で撃つ発数")]
	[SerializeField] int shotsPerBurst = 4;

	[Tooltip("斉射中の1発ごとの間隔（秒）")]
	[SerializeField] float shotIntervalSeconds = 0.16f;

	[Tooltip("斉射と斉射の間隔（秒）。長いほど弾幕の切れ目が広くなり、通り抜けやすくなる")]
	[SerializeField] float burstIntervalSeconds = 1.8f;

	/// <summary>
	/// 撃ち始めをずらす時間。
	/// 同じ設定の砲を並べると弾幕が揃ってしまうので、砲ごとにここでずらす
	/// </summary>
	[Tooltip("撃ち始めをずらす時間（秒）。砲ごとに変えると弾幕が揃わなくなる")]
	[SerializeField] float startDelaySeconds = 0f;

	/// <summary>
	/// 撃つ向きのばらつき。
	/// 0にすると毎回まったく同じ線を通るので機械的に見える。
	/// 大きくすると線がぼやけて「どこが危ないか」が読めなくなるため、ごく小さく取る
	/// </summary>
	[Tooltip("撃つ向きのばらつき（度）。大きくすると弾幕の線が読めなくなる")]
	[SerializeField, Range(0f, 10f)] float spreadDegrees = 0.4f;

	/// <summary>
	/// 撃ち始める距離。
	/// 機体は毎秒300で進むので、1200なら到達の4秒前から弾幕が見え始める。
	/// 近づいてから撃ち始めると、避ける場所を決める前に突っ込むことになる
	/// </summary>
	[Tooltip("機体がこの距離まで近づいたら撃ち始める。手前から弾幕を見せるため広めに取る")]
	[SerializeField] float activeDistance = 1200f;

	[Header("砲の位置を知らせる警告灯")]
	/// <summary>
	/// 点滅させる警告灯の見た目。
	///
	/// このプロジェクトは URP の追加ライトを切ってあるので、Light を置いても何も照らさない。
	/// 1を超える明るい色を出して Bloom で光らせる
	/// </summary>
	[Tooltip("点滅させる警告灯の見た目。追加ライトは効かないので、発光色とBloomで光らせる")]
	[SerializeField] Renderer beaconRenderer;

	[Tooltip("警告灯の色")]
	[SerializeField] Color beaconColor = new Color(1f, 0.12f, 0.08f, 1f);

	[Tooltip("警告灯の明るさ。Bloomのしきい値（1.1）を超えないと光って見えない")]
	[SerializeField] float beaconBrightness = 6f;

	[Tooltip("警告灯の点滅回数（1秒あたり）")]
	[SerializeField] float beaconBlinksPerSecond = 1.4f;

	[Tooltip("撃っていないときの警告灯の明るさの割合。0にすると消灯して位置が分からなくなる")]
	[SerializeField, Range(0f, 1f)] float beaconIdleRatio = 0.25f;

	[Header("射線の予告")]
	/// <summary>
	/// 弾が通る線をうっすら出す、細長い円柱。
	///
	/// 弾幕そのものは撃ってからでないと見えない。
	/// 撃ち始める前に線が見えていれば、その線を外して飛ぶという判断ができる。
	///
	/// 円柱は高さ2・直径1で作られているので、
	/// 長さの半分を y の大きさに、太さをそのまま x と z の大きさに入れる
	/// </summary>
	[Tooltip("弾が通る線を表す細長い円柱。未設定なら射線を出さない")]
	[SerializeField] Renderer fireLineRenderer;

	[Tooltip("射線を表示する長さ")]
	[SerializeField] float fireLineLength = 400f;

	[Tooltip("射線の太さ。細すぎると遠くで消えるので、画面に数ドット残る太さにする")]
	[SerializeField] float fireLineWidth = 1f;

	[Tooltip("射線の濃さ。撃つ直前はここまで濃くなる")]
	[SerializeField, Range(0f, 1f)] float fireLineAlpha = 0.35f;

	[Header("発砲音")]
	/// <summary>
	/// 発砲音。
	///
	/// 画面の外から撃たれても「撃たれている」と気づけるようにする。
	/// 目で追う手がかり（曳光弾・砲口の光・警告灯）は、そちらを見ていなければ分からない
	/// </summary>
	[Tooltip("発砲音。撃たれていることに気づく手がかりになる")]
	[SerializeField] AudioClip fireSound;

	[Tooltip("発砲音の音量")]
	[SerializeField, Range(0f, 1f)] float fireVolume = 0.5f;

	/// <summary>
	/// 発砲音が最大音量で聞こえる距離。
	/// これより遠いと、離れるほど小さくなる
	/// </summary>
	[Tooltip("発砲音が最大音量で聞こえる距離。遠いほど小さく鳴らす")]
	[SerializeField] float fireSoundFullVolumeDistance = 150f;

	/// <summary>次の1発を撃つ時刻</summary>
	float nextShotTime = 0f;

	/// <summary>この斉射で残っている発数。0なら斉射の合間</summary>
	int remainingInBurst = 0;

	/// <summary>撃つ相手ではなく、撃ち始める距離を測る基準。ステージごとに1機なので覚えておく</summary>
	PlaneController plane;

	/// <summary>ゲームが始まる前や終わった後は撃たない</summary>
	GameManager gameManager;

	/// <summary>発砲音の再生元。撃つたびに作り直さないように持っておく</summary>
	AudioSource fireAudioSource;

	/// <summary>警告灯の色を書き換える入れ物。マテリアルを複製しないために使う</summary>
	MaterialPropertyBlock beaconBlock;

	/// <summary>射線の濃さを書き換える入れ物</summary>
	MaterialPropertyBlock fireLineBlock;

	/// <summary>色を差し込むプロパティ。Lit と Unlit のどちらでも光るように両方入れる</summary>
	static readonly int Base_Color_Id = Shader.PropertyToID("_BaseColor");
	static readonly int Emission_Color_Id = Shader.PropertyToID("_EmissionColor");

	void Start()
	{
		if (barrel == null)
		{
			barrel = this.transform;
		}

		// 最初の斉射は、機体が撃ち始める距離に入ってから始まる。
		// ここでは砲ごとにずらすぶんだけ待たせておく
		nextShotTime = Time.time + startDelaySeconds;

		SetupFireLine();
	}

	void Update()
	{
		if (plane == null)
		{
			plane = FindInThisScene<PlaneController>();
			if (plane == null)
			{
				return;
			}
		}

		if (gameManager == null)
		{
			gameManager = FindInThisScene<GameManager>();
		}

		// プレイ中以外は撃たない。ゴール後やリザルト中に撃たれると理不尽になる
		bool isPlaying = gameManager != null && gameManager.IsPlay == true;
		bool isActive = isPlaying == true && IsPlaneNear() == true;

		UpdateBeacon(isActive);
		UpdateFireLine(isActive);

		if (isActive == false)
		{
			// 離れている間に斉射の途中で止まると、次に近づいたときに撃ち残しから始まってしまう
			remainingInBurst = 0;
			return;
		}

		if (Time.time < nextShotTime)
		{
			return;
		}

		if (remainingInBurst <= 0)
		{
			remainingInBurst = Mathf.Max(1, shotsPerBurst);
		}

		Fire();

		remainingInBurst = remainingInBurst - 1;
		nextShotTime = Time.time + (0 < remainingInBurst ? shotIntervalSeconds : burstIntervalSeconds);
	}

	/// <summary>弾が出る位置</summary>
	Vector3 MuzzlePosition => muzzle != null ? muzzle.position : barrel.position;

	/// <summary>弾が飛んでいく向き。砲身の向きそのもの</summary>
	Vector3 FireDirection => barrel.forward;

	/// <summary>
	/// 機体が撃ち始める距離まで近づいているか
	/// </summary>
	bool IsPlaneNear()
	{
		float distance = Vector3.Distance(plane.transform.position, barrel.position);
		return distance <= activeDistance;
	}

	/// <summary>
	/// 弾を1発撃つ
	/// </summary>
	void Fire()
	{
		if (bulletPrefab == null)
		{
			return;
		}

		Vector3 origin = MuzzlePosition;

		// 毎回まったく同じ線を通ると機械的に見えるので、ごくわずかにばらけさせる
		Vector3 spread = Random.insideUnitSphere * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
		Vector3 aim = (FireDirection + spread).normalized;

		GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(aim));

		AntiAirBullet bulletScript = bullet.GetComponent<AntiAirBullet>();
		if (bulletScript != null)
		{
			bulletScript.Launch(aim);
		}

		SpawnMuzzleFlash(origin, aim);
		PlayFireSound(origin);
	}

	/// <summary>
	/// 警告灯を点滅させる。
	///
	/// 砲は機体の通り道から35ユニット外、高さも通り道の下端より下に置いてあるので、
	/// 暗い背景に沈んで見つけられない。
	/// 点滅していれば、街灯や瓦礫と区別が付く
	/// </summary>
	/// <param name="isActive">撃っている最中かどうか</param>
	void UpdateBeacon(bool isActive)
	{
		if (beaconRenderer == null)
		{
			return;
		}

		float pulse = GetBeaconPulse();

		// 撃っていない間も薄く点けておく。完全に消すと近づくまで位置が分からない
		float ratio = isActive == true ? pulse : pulse * beaconIdleRatio;
		Color color = beaconColor * (beaconBrightness * ratio);
		color.a = 1f;

		if (beaconBlock == null)
		{
			beaconBlock = new MaterialPropertyBlock();
		}
		beaconRenderer.GetPropertyBlock(beaconBlock);
		beaconBlock.SetColor(Base_Color_Id, color);
		beaconBlock.SetColor(Emission_Color_Id, color);
		beaconRenderer.SetPropertyBlock(beaconBlock);
	}

	/// <summary>
	/// 警告灯の明るさ。
	/// ずっと同じ明るさで点いていると背景の光と見分けが付かないので、短く鋭く光らせる
	/// </summary>
	float GetBeaconPulse()
	{
		if (beaconBlinksPerSecond <= 0f)
		{
			return 1f;
		}

		float wave = Mathf.Sin(Time.time * beaconBlinksPerSecond * Mathf.PI * 2f);
		if (wave <= 0f)
		{
			return 0f;
		}

		// 4乗して山を細くする。半分の時間は消えていることになり、点滅として読める
		return wave * wave * wave * wave;
	}

	/// <summary>
	/// 射線の表示を用意する。
	/// 撃ち始めるまでは出さない
	/// </summary>
	void SetupFireLine()
	{
		if (fireLineRenderer == null)
		{
			return;
		}

		fireLineRenderer.enabled = false;
	}

	/// <summary>
	/// 弾が通る線を出す。
	/// 撃つ直前ほど濃くして、次の弾が来ることを知らせる
	/// </summary>
	/// <param name="isActive">撃っている最中かどうか</param>
	void UpdateFireLine(bool isActive)
	{
		if (fireLineRenderer == null)
		{
			return;
		}

		fireLineRenderer.enabled = isActive;
		if (isActive == false)
		{
			return;
		}

		Vector3 origin = MuzzlePosition;
		Vector3 direction = FireDirection;

		// 円柱は自分の +Y 方向へ伸びているので、その軸を撃つ向きに合わせる。
		// 位置は砲口と終点の真ん中。円柱は中心を原点にして作られている
		Transform line = fireLineRenderer.transform;
		line.rotation = Quaternion.FromToRotation(Vector3.up, direction);
		line.position = origin + direction * (fireLineLength * 0.5f);
		line.localScale = new Vector3(fireLineWidth, fireLineLength * 0.5f, fireLineWidth);

		Color color = beaconColor;
		color.a = fireLineAlpha * GetFireLineFade();

		if (fireLineBlock == null)
		{
			fireLineBlock = new MaterialPropertyBlock();
		}
		fireLineRenderer.GetPropertyBlock(fireLineBlock);
		fireLineBlock.SetColor(Base_Color_Id, color);
		fireLineRenderer.SetPropertyBlock(fireLineBlock);
	}

	/// <summary>
	/// 射線の濃さ。次の1発までの残り時間が短いほど濃くする
	/// </summary>
	float GetFireLineFade()
	{
		float remaining = nextShotTime - Time.time;
		if (remaining <= 0f)
		{
			return 1f;
		}

		// 濃くなり始める時間。斉射の合間はここより長いので、撃つ直前だけ濃くなる
		const float Fade_In_Seconds = 0.6f;
		return Mathf.Clamp01(1f - remaining / Fade_In_Seconds);
	}

	/// <summary>
	/// 発砲音を鳴らす。
	///
	/// AudioSource.PlayClipAtPoint は使わない。あれは毎回 GameObject を作って捨てるので、
	/// 砲が複数あるステージでは発砲のたびにゴミが出る。
	/// また3Dで鳴らすと、機体が毎秒300〜1500で飛ぶせいでドップラーと定位が暴れる。
	/// ここでは距離から音量だけを決めて2Dで鳴らす
	/// </summary>
	/// <param name="origin">弾が出た位置</param>
	void PlayFireSound(Vector3 origin)
	{
		if (fireSound == null || plane == null)
		{
			return;
		}

		float distance = Vector3.Distance(origin, plane.transform.position);
		float volume = fireVolume;
		if (fireSoundFullVolumeDistance > 0f && fireSoundFullVolumeDistance < distance)
		{
			// 距離に反比例させる。遠くの砲が同じ音量で鳴ると、どれに撃たれているか分からない
			volume = fireVolume * (fireSoundFullVolumeDistance / distance);
		}

		if (volume <= 0.01f)
		{
			return;
		}

		if (fireAudioSource == null)
		{
			fireAudioSource = this.gameObject.AddComponent<AudioSource>();
			fireAudioSource.playOnAwake = false;
			fireAudioSource.spatialBlend = 0f;
		}

		fireAudioSource.PlayOneShot(fireSound, volume);
	}

	/// <summary>
	/// 砲口を光らせる。撃った瞬間に砲の位置が分かる
	/// </summary>
	/// <param name="origin">弾が出る位置</param>
	/// <param name="direction">撃つ向き</param>
	void SpawnMuzzleFlash(Vector3 origin, Vector3 direction)
	{
		if (muzzleFlashPrefab == null)
		{
			return;
		}

		// 再生し終わればプレハブ側の設定（Stop Action = Destroy）で自分から消えるため、
		// ここで寿命を管理する必要はない
		GameObject flash = Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(direction), barrel);
		flash.transform.localScale = Vector3.one * muzzleFlashScale;
	}

	/// <summary>
	/// 同じステージにあるものだけを探す。
	///
	/// 次ステージは飛行中に裏で読み込んであり、そちらのシーンにも機体と GameManager が居る。
	/// シーンを見ないと、隣のステージの機体との距離で撃ち始めてしまう
	/// </summary>
	T FindInThisScene<T>() where T : Component
	{
		T[] found = Object.FindObjectsOfType<T>();
		for (int i = 0; i < found.Length; i++)
		{
			if (found[i].gameObject.scene == this.gameObject.scene)
			{
				return found[i];
			}
		}
		return null;
	}
}
