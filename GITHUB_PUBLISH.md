# Publishing GlocCamera_Engine on GitHub

## Prerequisites

- Git
- Visual Studio or MSBuild (to produce `Release` output)
- Optional: [GitHub CLI](https://cli.github.com/) (`gh`) for `gh release create`

## 1) Package a release zip (local)

From the **repository root** in PowerShell:

```powershell
.\scripts\package-release.ps1
```

This reads the version from `GlocCamera_Engine\GlocCameraPlugin.cs` (`PluginVersion`), expects `GlocCamera_Engine\bin\Release\GlocCamera_Engine.dll` to exist, and writes:

`release\GlocCamera_Engine_v<version>.zip`

containing the DLL and `release\INSTALL.txt`.

Build **Release** first (Visual Studio or):

```powershell
msbuild .\GlocCamera_Engine\GlocCamera_Engine.csproj /p:Configuration=Release
```

## 2) Create the GitHub repository

Create a new **public** repo (e.g. `GlocCamera_Engine`). Do **not** add a README or license on GitHub if you already have them locally.

## 3) First push

```powershell
cd C:\Users\at747\source\repos\GlocCamera_Engine
git init
git add -A
git commit -m "Initial release: G-LOC Camera for Nuclear Option (v1.2.0)"
git branch -M main
git remote add origin https://github.com/<YOUR_USER>/GlocCamera_Engine.git
git push -u origin main
```

If `origin` already exists:

```powershell
git remote set-url origin https://github.com/<YOUR_USER>/GlocCamera_Engine.git
git push -u origin main
```

## 4) Tag and GitHub Release

Align the tag with `[BepInPlugin(..., "x.y.z")]` and `AssemblyInfo` **FileVersion**.

```powershell
git tag -a v1.2.0 -m "v1.2.0"
git push origin v1.2.0
```

Create a release and attach the zip (replace path if your version differs):

```powershell
gh release create v1.2.0 "release\GlocCamera_Engine_v1.2.0.zip" --title "v1.2.0 — G-LOC Camera" --notes-file CHANGELOG.md
```

Or create the release manually on GitHub and upload `release\GlocCamera_Engine_v1.2.0.zip`.

## Version checklist

- `GlocCameraPlugin.PluginVersion`
- `Properties\AssemblyInfo.cs` (`AssemblyVersion` / `AssemblyFileVersion`)
- Tag name `v#.#.#`
- Release asset name `GlocCamera_Engine_v#.#.#.zip` (from script)

## Disclaimer

This mod is an independent fan project and is not affiliated with or endorsed by the developers of **Nuclear Option**.
