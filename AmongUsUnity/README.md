# Among Us LAN (MVP, Unity 6)

Clean C# reimplementation of the gameplay loop (the original IL2CPP method bodies are not recoverable),
reusing original sprites and audio from the local export. Unity **6000.6.4f1**, no extra packages.

## Prepare
1. `python3 tools/sync_mvp_assets.py` (from repo root, after exporting with AssetRipper into `RecoveredMac/`).
2. Open `AmongUsUnity/` in Unity 6 or build from the CLI:
   `Unity -batchmode -nographics -quit -projectPath AmongUsUnity -executeMethod BuildScript.BuildMac` (or `BuildWindows`).

## Play
One player presses **Crear partida**; the others see it in the list (UDP discovery, port 47778) or join by IP
(TCP, port 47777). Allow both ports in the firewall.
WASD/arrows move · hold **E** to do a task · **Q** kill (impostor) · **R** report body · **F** emergency button.

## Implemented
Host-authoritative lobby, roles (1 impostor, 2 from 7 players), tasks with shared progress bar, kills with
cooldown and bodies, report/emergency meetings, voting, ejection, win conditions, return to lobby.

## Not yet
Original Skeld map and animations, vision/fog, sabotages, vents, chat, cosmetics, reconnect, real task minigames.

## Test automation
`-autohost`, `-autojoin -ip <ip>`, `-bot`, `-four`, `-slowkill`, `-name <n>` run bots (see `Game.cs`).
