# SteamCMD Workshop publish (Space Engineers)

Space Engineers uses Steam **AppID `244850`**. You can **update an existing Workshop item** (or create a new one) with SteamCMD and a small VDF descriptor.

## Prerequisites

1. Install [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD).
2. Use a Steam account that **owns Space Engineers** and is allowed to edit the Workshop item (your mod’s owner).
3. First login may require **Steam Guard**; complete it once on that machine (or use a [sentry file](https://developer.valvesoftware.com/wiki/SteamCMD#Logging_in) for automation).

## One-time setup

1. Copy the example VDF for the mod you publish:
   - `se_upgrade_workshop.vdf.example` → `se_upgrade_workshop.vdf` (gitignored name recommended: keep secrets out of commits).
2. Edit the copy:
   - Set **`publishedfileid`** to your item ID (Upgrade mod in this repo: `3341019311`), or **`0`** for a **new** item.
   - Set **`contentfolder`** to the **absolute** path of the **mod root folder** (the folder that contains `Data/` and `metadata.mod`), e.g. `...\SE_mod\SE_Upgrade_module_mod`.
   - Set **`previewfile`** to an absolute path of a preview image (JPG/PNG per Steam rules).
   - Adjust **`title`**, **`description`**, **`changenote`**, **`visibility`** as needed.

Paths in the VDF must be valid on the machine that runs SteamCMD (use doubled backslashes `\\` on Windows inside the file).

## Publish / update command

```text
steamcmd +login YOUR_STEAM_USERNAME YOUR_PASSWORD +workshop_build_item "D:\full\path\to\se_upgrade_workshop.vdf" +quit
```

Prefer **not** putting the password in scripts. For a one-off machine, interactive login is fine. The example `upload-example.ps1` can read **`STEAM_PASS`** only if you set it in the environment (e.g. CI secrets); never commit passwords.

## Dedicated server: download mods (not upload)

To **pull** Workshop content to a server (no upload rights needed):

```text
steamcmd +login anonymous +workshop_download_item 244850 WORKSHOP_FILE_ID +quit
```

Install path defaults under the SteamCMD `steamapps/workshop/content/244850/...`; point your server’s mod list at those files or copy into the game `Mods` layout your host expects.

## This repository

| File | Purpose |
|------|---------|
| `se_upgrade_workshop.vdf.example` | Descriptor for **SE_Upgrade_module_mod** (known Workshop ID in README/modinfo). |
| `se_prime_workshop.vdf.example` | Template for **SE_Prime_Block_mod**; set `publishedfileid` after you create the item once (in-game or with `publishedfileid` `0`). |
| `upload-example.ps1` | Optional wrapper: resolves repo paths and calls `workshop_build_item` (set `SteamCmd` path and credentials yourself). |

Official SteamCMD workshop build behavior can change; if a command fails, check the latest [SteamCMD wiki](https://developer.valvesoftware.com/wiki/SteamCMD) and Space Engineers community guides for `workshop_build_item`.

## 한 번에 두 모드 배포 (로컬)

비밀번호는 저장소에 넣지 마세요.

**대화형 로그인(권장):** 계정명·비밀번호·Steam Guard 코드를 실행 중 입력합니다. 비밀번호는 `Read-Host -AsSecureString`으로 숨깁니다.

```powershell
cd D:\Documents\SE_mod\tools\steamcmd
.\Deploy-Workshop.ps1
```

**비대화형(CI 등):** 환경 변수만 사용합니다.

```powershell
$env:STEAM_USER = "스팀_로그인명"
$env:STEAM_PASS = "비밀번호"
# 선택: $env:STEAM_GUARD = "가드코드"
.\Deploy-Workshop.ps1
```

- **업그레이드 모드**: 워크숍 ID `3341019311`으로 갱신합니다.
- **프라임 모드**: 기본 `STEAM_PRIME_PUBLISHED_ID`가 없으면 **`0`**(새 항목 생성)입니다. 첫 업로드 후 SteamCMD 출력에 나오는 **FileID**를 받아 다음부터 `$env:STEAM_PRIME_PUBLISHED_ID = "그번호"` 로 설정하세요. 이미 워크숍에 올린 항목이 있으면 처음부터 그 번호를 넣으면 됩니다.

미리보기는 `workshop_preview.png`(1×1 플레이스홀더)를 씁니다. 거절되면 512×512 등 가이드에 맞는 이미지로 교체하세요.
