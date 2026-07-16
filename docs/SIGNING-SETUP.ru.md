# Подпись пакета (один раз)

CI падает, если в GitHub **нет трёх секретов** Unity. Без них подписать пакет нельзя — это не баг workflow.

## 1. Unity Cloud — service account

1. Открой [cloud.unity.com](https://cloud.unity.com) → организация **jelewow**
2. **Administration → Service accounts** → создай account с правом **Package signing**
3. Сохрани **Key ID** и **Key Secret** (Secret показывается один раз)
4. **Organization Settings** → скопируй **Organization ID** (UUID, не slug)

## 2. GitHub Secrets

Репозиторий: `Jelewow/unity-google-localization`  
**Settings → Secrets and variables → Actions → New repository secret**

| Имя | Значение |
|-----|----------|
| `UPM_SERVICE_ACCOUNT_KEY_ID` | Key ID из Unity |
| `UPM_SERVICE_ACCOUNT_KEY_SECRET` | Key Secret из Unity |
| `UPM_ORG_ID` | Organization ID (UUID) |

Или из PowerShell (подставь свои значения):

```powershell
gh secret set UPM_SERVICE_ACCOUNT_KEY_ID -R Jelewow/unity-google-localization --body "PASTE_KEY_ID"
gh secret set UPM_SERVICE_ACCOUNT_KEY_SECRET -R Jelewow/unity-google-localization --body "PASTE_KEY_SECRET"
gh secret set UPM_ORG_ID -R Jelewow/unity-google-localization --body "PASTE_ORG_UUID"
```

## 3. Перезапуск CI

После секретов:

```powershell
gh run rerun 29509018071 -R Jelewow/unity-google-localization
```

Или: **Actions → Sign and release → Run workflow** (кнопка появится после push workflow с `workflow_dispatch`).

## 4. OpenUPM (signed tarball)

Дождись merge PR: https://github.com/openupm/openupm/pull/6699  
(`trackingMode: githubRelease` — OpenUPM берёт signed `.tgz` из GitHub Release, а не из git).

## 5. turtle-clicker

Когда на OpenUPM появится **1.0.2 signed**:

```json
"com.jelewow.unity-sheets-localization": "1.0.2"
```

Предупреждение *installed without a signature* исчезнет.
