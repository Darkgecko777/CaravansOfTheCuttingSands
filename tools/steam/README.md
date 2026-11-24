# Upload the Windows player to Steam

In the Unity Editor, run **Caravans/Build Windows**. That writes `Builds/Windows/Caravans of the Cutting Sands.exe` and copies the play data beside it. The build menu removes `steam_appid.txt` on purpose. Steam supplies the app ID when the game is launched from the library.

You can put your Steam username on the first line of `tools/steam/steam_username.txt`. That file is gitignored. Do not commit it. The script never reads a password from a file or an environment variable.

From the repo root:

```
powershell -File tools/steam/Upload-ToSteam.ps1
```

A first check can use `-DryRun`. A dry run writes the VDFs under `tools/steam/generated/` and does not download SteamCMD, start it, or ask for a password.

On a live run, if `tools/steam/steamcmd/steamcmd.exe` is missing, the script downloads Valve's official zip into `tools/steam/steamcmd/`. Type your Steam password and Steam Guard code into that same window. There is no unattended or scheduled upload, and no bot holds a Steam password.

Both apps are uploaded in one login. Pass `-Only main` or `-Only playtest` to upload one of them. The same `Builds/Windows` player goes to both.

Confirmed IDs: main app 4765690 / depot 4765692; Playtest app 4765820 / depot 4765822. The Playtest app is a separate Steam app, not a branch of the main app.

On **Installation > General** for both apps, the launch-option executable must be exactly `Caravans of the Cutting Sands.exe` (spaces included), Windows, 64-bit. The install folder should be `Caravans of the Cutting Sands`.

SteamCMD leaves `SetLive` empty. After the upload, set the builds live on the Steamworks site and then Publish. Set the Playtest app's build live right away. Set the main app's **default** branch live when that build is the one for review.

- Main builds: https://partner.steamgames.com/apps/builds/4765690
- Playtest builds: https://partner.steamgames.com/apps/builds/4765820

Clear vestigial old-project-name pieces still in Steamworks settings: depot name, install folder, launch-option executable, old build descriptions, and branch names. The built exe and `app.info` already say `Caravans of the Cutting Sands` / `Uncanny Mercantile`.
