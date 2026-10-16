# Hay un impostor entre nosotros (Unity 6, LAN)

Reimplementación en C# del bucle de juego (los cuerpos de método IL2CPP originales no son recuperables),
sobre el mapa Skeld, sprites, audio y minijuegos originales migrados con sus GUID. Unity **6000.6.4f1**.

## Preparar (una vez, desde la raíz del repo)
1. Exportar con AssetRipper a `RecoveredMac/` (ver `MIGRACION_MAC.md`).
2. `python3 tools/sync_mvp_assets.py` (audio/arte pequeño) y
   `python3 tools/migrate_map.py Ship/SkeldShip.prefab GameObject/Lobby.prefab GameObject/ShipMap.prefab ... --sprites AmongUsUnity/Assets/Editor/ui_sprites.txt`
   (ver la lista completa de raíces en el historial de commits / `prefabs.txt`).
3. Compilar: `Unity -batchmode -nographics -quit -projectPath AmongUsUnity -executeMethod BuildScript.BuildMac` (o `BuildWindows`).

## Jugar
Uno pulsa **Crear partida** y comparte el **código de sala** de 7 letras que aparece arriba en la sala.
Los demás pulsan **Unirse con código** (o la partida aparece sola en la lista). Puertos: TCP 47777, UDP 47778.
WASD/flechas mover · E usar · Q matar · R reportar · V ventilar · Tab mapa · Enter chat.

## Incluido
Sala (dropship) con colores y ajustes de partida, roles, 17 tareas con minijuegos originales, sabotajes
(reactor, O2, luces, comunicaciones, puertas), ventilas, cámaras de seguridad, mapa/admin, reuniones con
votación y chat, expulsión, niebla de visión, sonido ambiente, ES/EN.
