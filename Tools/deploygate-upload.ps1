<#
.SYNOPSIS
    ビルド済みのAPKをDeployGateにアップロードする。

.DESCRIPTION
    認証情報は環境変数から読み込む。APIキーをこのスクリプトに直接書かないこと。

      DEPLOYGATE_API_KEY    : DeployGateのAPIキー
                              https://deploygate.com/settings （API Key の項目）から取得する
      DEPLOYGATE_OWNER_NAME : アップロード先のユーザー名またはプロジェクト名

    設定例（現在のPowerShellセッションのみ有効）:
      $env:DEPLOYGATE_API_KEY = "取得したキー"
      $env:DEPLOYGATE_OWNER_NAME = "あなたのユーザー名"

    恒久的に設定する場合:
      [Environment]::SetEnvironmentVariable("DEPLOYGATE_API_KEY", "取得したキー", "User")

    APIキーはcurlのコマンドライン引数には渡さず、一時的な設定ファイル経由で渡している。
    コマンドライン引数はタスクマネージャ等から他プロセスに見えてしまうため。

.PARAMETER ApkPath
    アップロードするAPKのパス。省略時はビルド出力フォルダ内で最も新しい.apkを自動で選ぶ。

.PARAMETER Message
    アップロードの説明。省略時はgitのコミットハッシュとブランチ名から生成する。

.PARAMETER DistributionName
    配布ページ名。省略時は固定名 "HarryFly2" を使う。空文字を明示すると配布ページを作らない。
    ブランチごとにページを分けたい場合だけ明示的に渡すこと。
    ただしDeployGateの無料プランは配布ページ数に上限があるので、分けすぎると配信できなくなる。

.PARAMETER ReleaseNote
    アプリ更新時にテスターへ表示されるメッセージ。

.PARAMETER BuildDir
    APKを探すフォルダ。省略時はリポジトリの兄弟フォルダ HarryFly2_Build。

.EXAMPLE
    .\Tools\deploygate-upload.ps1

.EXAMPLE
    .\Tools\deploygate-upload.ps1 -ReleaseNote "アイテム取得時の振動に対応"
#>
[CmdletBinding()]
param(
	[string]$ApkPath = "",
	[string]$Message = "",
	[string]$DistributionName = "",
	[string]$ReleaseNote = "",
	[string]$BuildDir = ""
)

$ErrorActionPreference = "Stop"

<#
    既定の配布ページ名。
    ここを固定にしておくことで、何回配信しても配布ページは1枚のままになり、
    テスターに配ったURLも変わらない。
#>
$Default_Distribution_Name = "HarryFly2"

# ---- 認証情報の確認 -------------------------------------------------------

# Windowsのユーザー環境変数は、設定後に起動したプロセスにしか引き継がれない。
# 親プロセスが古い環境を持っている場合に備え、Process → User → Machine の順で探す。
function Get-EnvValue {
	param([string]$Name)

	foreach ($scope in @("Process", "User", "Machine")) {
		$value = [Environment]::GetEnvironmentVariable($Name, $scope)
		if (-not [string]::IsNullOrWhiteSpace($value)) {
			return $value.Trim()
		}
	}

	return $null
}

$apiKey = Get-EnvValue "DEPLOYGATE_API_KEY"
$ownerName = Get-EnvValue "DEPLOYGATE_OWNER_NAME"

$missing = @()
if ([string]::IsNullOrWhiteSpace($apiKey)) { $missing += "DEPLOYGATE_API_KEY" }
if ([string]::IsNullOrWhiteSpace($ownerName)) { $missing += "DEPLOYGATE_OWNER_NAME" }

if ($missing.Count -gt 0) {
	Write-Error @"
環境変数が未設定です: $($missing -join ', ')

DeployGateのAPIキーは https://deploygate.com/settings から取得できます。
取得したら、以下を実行してください（値はご自身で入力してください）:

  [Environment]::SetEnvironmentVariable("DEPLOYGATE_API_KEY", "取得したキー", "User")
  [Environment]::SetEnvironmentVariable("DEPLOYGATE_OWNER_NAME", "あなたのユーザー名", "User")
"@
	exit 1
}

# よくある間違い: DeployGateの画面の "API Key : " というラベルごとコピーしてしまう
if (-not $apiKey.StartsWith("deploygate_")) {
	Write-Error @"
DEPLOYGATE_API_KEY の値が正しくないようです（"deploygate_" で始まっていません）。

DeployGateの画面に表示されている "API Key : " はラベルなので、
その右側の deploygate_ から始まる文字列だけを設定してください。

  誤: API Key : deploygate_xxxx...
  正: deploygate_xxxx...
"@
	exit 1
}

# ---- APKの決定 ------------------------------------------------------------

$repoRoot = Split-Path $PSScriptRoot -Parent

if ([string]::IsNullOrWhiteSpace($BuildDir)) {
	$BuildDir = Join-Path (Split-Path $repoRoot -Parent) "HarryFly2_Build"
}

if ([string]::IsNullOrWhiteSpace($ApkPath)) {
	if (-not (Test-Path $BuildDir)) {
		Write-Error "ビルド出力フォルダが見つかりません: $BuildDir`n-ApkPath でAPKを直接指定してください。"
		exit 1
	}

	$latest = Get-ChildItem -Path $BuildDir -Filter "*.apk" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
	if ($null -eq $latest) {
		Write-Error "APKが見つかりません: $BuildDir"
		exit 1
	}

	$ApkPath = $latest.FullName
	Write-Host "APKを自動選択しました（最終更新が最も新しいもの）" -ForegroundColor DarkGray
}

if (-not (Test-Path $ApkPath)) {
	Write-Error "APKが見つかりません: $ApkPath"
	exit 1
}

$apkItem = Get-Item $ApkPath
$apkSizeMiB = [math]::Round($apkItem.Length / 1MB, 1)

# ---- git情報からデフォルト値を作る ---------------------------------------

$gitHash = ""
$gitBranch = ""
try {
	Push-Location $repoRoot
	$gitHash = (git rev-parse --short HEAD)
	$gitBranch = (git rev-parse --abbrev-ref HEAD)
	$isDirty = (git status --porcelain)
	Pop-Location
}
catch {
	# gitが無い/リポジトリでない場合はデフォルト値なしで続行する
	if ((Get-Location).Path -ne $repoRoot) { Pop-Location -ErrorAction SilentlyContinue }
}

if ([string]::IsNullOrWhiteSpace($Message)) {
	$Message = "git:$gitHash branch:$gitBranch"
	if (-not [string]::IsNullOrWhiteSpace($isDirty)) {
		$Message = $Message + " (未コミットの変更あり)"
	}
}

# 省略時は固定名を使う。"" を明示された場合は配布ページを作らない。
#
# 以前はブランチ名を既定にしていたが、DeployGateの distribution_name は
# 「同名があれば更新、無ければ新規作成」という動作なので、
# ブランチを切り替えるたびに配布ページが1枚ずつ増えていき、
# 無料プランの上限（The distribution count limit exceeded）に達して配信できなくなった。
# ページを分ける必要がないなら固定名のほうが運用が楽で、
# テスターに配ったURLも変わらない。どのブランチのビルドかは message に残る。
#
# [string] 型の引数は $null を代入しても空文字になるため、既定値との比較では
# 「省略された」のか「空文字を明示された」のかを区別できない。
# 引数が渡されたかどうかで判定する
if (-not $PSBoundParameters.ContainsKey("DistributionName")) {
	$DistributionName = $Default_Distribution_Name
}

# ---- 確認表示 -------------------------------------------------------------

Write-Host ""
Write-Host "DeployGate にアップロードします" -ForegroundColor Cyan
Write-Host "  APK        : $ApkPath ($apkSizeMiB MiB)"
Write-Host "  アップロード先 : $ownerName"
Write-Host "  message    : $Message"
if (-not [string]::IsNullOrWhiteSpace($DistributionName)) {
	Write-Host "  配布ページ    : $DistributionName"
}
if (-not [string]::IsNullOrWhiteSpace($ReleaseNote)) {
	Write-Host "  release_note : $ReleaseNote"
}
Write-Host ""

# ---- アップロード ---------------------------------------------------------

# APIキーをコマンドライン引数に載せないよう、curlの設定ファイル経由で渡す
$configPath = Join-Path $env:TEMP ("dg-" + [guid]::NewGuid().ToString("N") + ".conf")

try {
	# curlの設定ファイル形式。ダブルクォート内のバックスラッシュはエスケープが要るがトークンには通常含まれない
	Set-Content -Path $configPath -Value ('header = "Authorization: Bearer ' + $apiKey + '"') -Encoding ascii -NoNewline

	$curlArgs = @(
		"--silent", "--show-error",
		"--config", $configPath,
		"--url", "https://deploygate.com/api/users/$ownerName/apps",
		"-X", "POST",
		"-F", "file=@$ApkPath"
	)

	if (-not [string]::IsNullOrWhiteSpace($Message)) {
		$curlArgs += @("--form-string", "message=$Message")
	}
	if (-not [string]::IsNullOrWhiteSpace($DistributionName)) {
		$curlArgs += @("--form-string", "distribution_name=$DistributionName")
	}
	if (-not [string]::IsNullOrWhiteSpace($ReleaseNote)) {
		$curlArgs += @("--form-string", "release_note=$ReleaseNote")
	}

	Write-Host "アップロード中..." -ForegroundColor DarkGray
	$response = & curl.exe @curlArgs
	$curlExit = $LASTEXITCODE
}
finally {
	if (Test-Path $configPath) {
		Remove-Item $configPath -Force
	}
}

if ($curlExit -ne 0) {
	Write-Error "curlが失敗しました (exit code: $curlExit)"
	exit 1
}

if ([string]::IsNullOrWhiteSpace($response)) {
	Write-Error "DeployGateから空の応答が返りました。"
	exit 1
}

# ---- 応答の解釈 -----------------------------------------------------------

try {
	$json = $response | ConvertFrom-Json
}
catch {
	Write-Error "DeployGateの応答をJSONとして解釈できませんでした:`n$response"
	exit 1
}

if ($json.error -eq $true) {
	Write-Host ""
	Write-Host "アップロードに失敗しました" -ForegroundColor Red
	if ($json.message) { Write-Host "  message : $($json.message)" }
	if ($json.because) { Write-Host "  because : $($json.because)" }
	exit 1
}

$r = $json.results

Write-Host ""
Write-Host "アップロード成功" -ForegroundColor Green
if ($r.name) { Write-Host "  アプリ名   : $($r.name)" }
if ($r.package_name) { Write-Host "  パッケージ : $($r.package_name)" }
if ($r.version_name) { Write-Host "  バージョン : $($r.version_name) (versionCode: $($r.version_code))" }
if ($r.path) { Write-Host "  アプリページ : https://deploygate.com$($r.path)" }
if ($r.distribution -and $r.distribution.url) {
	Write-Host "  配布ページ   : $($r.distribution.url)" -ForegroundColor Cyan
	Write-Host "  （このURLをテスターに共有してください）" -ForegroundColor DarkGray
}
Write-Host ""
