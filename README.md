# SwitchDesktops

A lightweight virtual-desktop switcher for Windows with a crossfade transition. Assign running windows to "desktops," then flip between them with hotkeys.

**This does NOT use Windows' built-in virtual desktops.** It manages its own desktop model by showing/hiding window sets, which lets us capture screenshots of each state and animate a real crossfade between them.

## What you need

- **Windows 10 (build 19041+) or Windows 11** — cannot be built or run on macOS/Linux.
- **.NET 8 SDK** — download from https://dotnet.microsoft.com/download
- Either:
  - **Visual Studio 2022** (Community is free) with the ".NET desktop development" workload, OR
  - **JetBrains Rider**, OR
  - **VS Code** + the C# Dev Kit extension + the .NET 8 SDK on your PATH

## Building on a Mac

You cannot. WPF requires Windows at both compile and runtime. Your options:

1. **Parallels / VMware Fusion / UTM** running Windows 11 (ARM works on Apple Silicon)
2. **A Windows PC** (physical machine or mini-PC — best for testing window management)
3. **Remote Windows machine** via RDP (spare PC, or a cloud VM)

You can edit the source on the Mac (IntelliSense works in Rider/VS Code), then build and run on Windows.

## Build & run

From a Windows command prompt or PowerShell in the repo root:

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project SwitchDesktops
```

Or open `SwitchDesktops.sln` in Visual Studio / Rider and press F5.

## Usage

On startup, the app:
1. Enumerates all currently visible top-level windows and assigns them to **Desktop 1** (the active desktop).
2. Registers global hotkeys and creates a tray icon.

**Hotkeys:**
- `Ctrl+Alt+1` — switch to Desktop 1
- `Ctrl+Alt+2` — switch to Desktop 2
- `Ctrl+Alt+3` — switch to Desktop 3
- `Ctrl+Alt+M` — open the "move window to desktop" picker

**Tray icon:** right-click for a menu with the same actions plus Exit.

**Moving a window:** open the picker (Ctrl+Alt+M or tray → "Move window…"), select a window, pick a target desktop, hit Move. If the target isn't the active desktop, the window is hidden immediately.

**Switching:** hides all windows on the current desktop, shows all windows on the target desktop, and animates a ~280ms crossfade over the top so you don't see the flash.

## Known limitations

- **GPU-composited windows (Chrome, Edge, Electron apps, VS Code, Discord) may render black in the crossfade snapshot.** Their real window still shows/hides correctly — only the transition frame is affected. To fix this properly, swap `ScreenCapture` to use the Windows.Graphics.Capture API. Left as a follow-up.
- **New windows opened while the app is running** are absorbed into whichever desktop is active when you next switch. There's no live watcher yet.
- **Fullscreen exclusive games** may fight the topmost overlay. The app won't crash, but the transition will look wrong on top of them.
- **Focus restoration** uses the AttachThreadInput trick — usually reliable but not 100%. If focus feels off, click the window you want.
- **Multi-monitor:** the crossfade overlay only covers the primary monitor for now. Windows on other monitors still hide/show correctly.
- **UAC-elevated windows** cannot be manipulated by a non-elevated process. Run the app as admin if you need to manage them.

## Architecture

```
SwitchDesktops/
├── Interop/
│   └── NativeMethods.cs        # P/Invoke to user32, dwmapi, gdi32, kernel32
├── Core/
│   ├── ManagedWindow.cs        # HWND + metadata
│   ├── Desktop.cs              # A desktop = ordered set of ManagedWindows
│   └── DesktopManager.cs       # Holds all desktops, tracks active
├── Services/
│   ├── WindowTracker.cs        # EnumWindows → filtered list of user windows
│   ├── ScreenCapture.cs        # PrintWindow + CopyFromScreen → BitmapSource
│   ├── DesktopSwitcher.cs      # Orchestrates: capture → hide/show → crossfade → refocus
│   └── HotkeyService.cs        # RegisterHotKey via message-only HwndSource
├── UI/
│   ├── CrossfadeOverlay.xaml   # Fullscreen topmost transparent window
│   └── MoveWindowPicker.xaml   # Lists windows, target desktop, Move/Cancel
├── App.xaml                    # Application entry
├── App.xaml.cs                 # Wires everything together, tray icon
└── app.manifest                # PerMonitorV2 DPI + Win10/11 supported OS
```

## Packaging & distribution (MSIX + .appinstaller)

The app is set up for **MSIX packaging with `.appinstaller` auto-updates**. Users install by clicking one link; the app then checks for updates on every launch and installs them silently in the background.

### One-time setup (Windows only)

0. **Install the Windows 10/11 SDK.** This is required before you can sign the MSIX or build the package — `build-msix.ps1` needs `makeappx.exe` and `signtool.exe`, which only ship with the SDK. Just the SDK is needed; Visual Studio is *not* required, since `build-msix.ps1` drives those tools directly instead of Visual Studio's single-project MSIX tooling.

   **Option A — winget (recommended):**
   ```powershell
   winget install Microsoft.WindowsSDK.10.0.26100
   ```

   **Option B — manual download**, if winget isn't available or that package ID has changed: go to https://developer.microsoft.com/windows/downloads/windows-sdk/, download the standalone installer, run it, and when prompted for features you only need **"Windows SDK Signing Tools for Desktop Apps"** and **"MSIX Packaging Tool"** (uncheck the rest to keep it quick).

   **Verify it installed correctly** — the scripts auto-discover the SDK under `C:\Program Files (x86)\Windows Kits\10\bin`, so no PATH changes are needed, but you can sanity-check with:
   ```powershell
   Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -Filter '10.0.*'
   ```
   You should see at least one version folder (e.g. `10.0.26100.0`) containing an `x64\makeappx.exe` and `x64\signtool.exe`. If nothing shows up, the SDK either didn't install or installed to a nonstandard location — rerun the installer and make sure "Windows SDK Signing Tools for Desktop Apps" is checked.

1. **Create a self-signed certificate** (from an elevated PowerShell in the repo root):
   ```powershell
   pwsh ./scripts/create-self-signed-cert.ps1
   ```
   This writes `SwitchDesktops/SwitchDesktops_TemporaryKey.pfx` (used to sign the package — *never commit this*) and `deploy/SwitchDesktops.cer` (public cert users import to trust it).

2. **Generate placeholder icons** (only needed once, or if you want to refresh):
   ```powershell
   pwsh ./scripts/generate-placeholder-assets.ps1
   ```
   Replace the PNGs in `SwitchDesktops/Assets/` with real icons before shipping. (The tray icon itself is separate — see `scripts/generate-tray-icon.ps1` — and is already tracked in git so `dotnet run` shows a real tray icon with no setup.)

3. **Set the base URL** in two places to match where you'll host it:
   - `deploy/SwitchDesktops.appinstaller` — replace `https://YOUR-USERNAME.github.io/switch-desktops/`
   - `SwitchDesktops/SwitchDesktops.csproj` — same value in `<AppInstallerUri>`

### Build the MSIX locally

```powershell
pwsh ./scripts/build-msix.ps1 -Version 1.0.0.0
```

Output lands in `deploy/`: a signed `SwitchDesktops_1.0.0.0_x64.msix` plus the `.appinstaller` and `.cer`.

To test-install it locally: `certutil -addstore -f "TrustedPeople" deploy\SwitchDesktops.cer`, then `Add-AppxPackage -Path deploy\SwitchDesktops_1.0.0.0_x64.msix`.

### Install locally for testing

Because the cert is self-signed, Windows won't trust it by default. Import it once:

```powershell
certutil -addstore -f "TrustedPeople" deploy\SwitchDesktops.cer
```

Then double-click `deploy\SwitchDesktops.appinstaller`. Windows will show the installer UI and install per-user. Uninstall via Settings → Apps.

### Deploy via GitHub Pages + Releases

The workflow at `.github/workflows/release.yml` does the whole pipeline when you push a tag `vX.Y.Z`:

1. Decodes your signing PFX from a repo secret.
2. Builds and signs the MSIX at that version.
3. Rewrites the `.appinstaller` URLs and version to match.
4. Publishes `.msix`, `.appinstaller`, `.cer`, and `index.html` to GitHub Pages.
5. Attaches the artifacts to the GitHub Release for that tag.

**Repo setup (one time):**

- **Settings → Secrets and variables → Actions**:
  - `SIGNING_PFX_BASE64` (secret) — output of `[Convert]::ToBase64String([IO.File]::ReadAllBytes('SwitchDesktops_TemporaryKey.pfx'))`
  - `SIGNING_PFX_PASSWORD` (secret) — password for that PFX
  - `APPINSTALLER_BASE_URL` (variable) — `https://<user>.github.io/<repo>/` (trailing slash)
- **Settings → Pages** → Source: **GitHub Actions**.

**Cutting a release:**

```bash
git tag v1.0.1
git push origin v1.0.1
```

Users visit `https://<user>.github.io/<repo>/` and click **Install**. Their machine polls the same URL on every launch and auto-updates.

### When you're ready to distribute widely

Swap the self-signed cert for either:

- **Azure Trusted Signing** (~$10/mo) — no hardware token, integrates with GitHub Actions via the `azure/trusted-signing-action`. Best value.
- **Traditional code-signing cert** — DigiCert/Sectigo/etc. EV certs give SmartScreen reputation faster but require a hardware token (awkward in CI).

Either way, you replace the PFX secret and everything else in this pipeline stays the same.

## Extending

- **More desktops:** `new DesktopManager(initialCount: 5)` in `App.xaml.cs`.
- **Different hotkeys:** change the `VK_*` constants and the `Register` calls in `App.xaml.cs`.
- **Slide instead of fade:** swap the animation in `CrossfadeOverlay.RunAsync` for a `TranslateTransform` on the images.
- **Per-monitor overlay:** enumerate `System.Windows.Forms.Screen.AllScreens` and create one overlay per screen.
- **Better capture for Chromium:** replace `ScreenCapture` with a wrapper around `Windows.Graphics.Capture` (WinRT — reference `Microsoft.Windows.CsWinRT` and target `net8.0-windows10.0.19041.0`, which is already set).
