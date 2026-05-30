# Publishing GlocCamera to GitHub

**Repo:** https://github.com/Mursisru/GlocCamera  
**Git clone (push):** `C:\Users\at747\OneDrive\Desktop\GITHUB local\CGLOCCAMERA\GlocCamera`

## Sync from engine

```powershell
$src = "C:\Users\at747\source\repos\GlocCamera_Engine"
$dst = "C:\Users\at747\OneDrive\Desktop\GITHUB local\CGLOCCAMERA\GlocCamera"
robocopy $src $dst /MIR /XD bin obj .vs .git
```

## Build & zip

```powershell
cd $src
# Build Release in Visual Studio or MSBuild
.\scripts\package-release.ps1
```

Zip: `release\GlocCamera_Engine_vX.Y.Z.zip` (DLL + INSTALL.txt). DLL archives in `release\` are gitignored.

## Commit & push

```powershell
cd $dst
git status
git add -A
git commit -m "G-LOC Camera 2.0.8: cinematic atmosphere, realistic NVG"
git push origin main
```

Use **WPF Auto Git Helper** or `gh` for releases.
