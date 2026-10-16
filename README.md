# Entre nosotros hay un impostor

Herramientas e informes para recuperar localmente la instalación de Steam de **Among Us**, App ID `945360`,
build `25134893`. Motor detectado: **Unity 2022.3.44f1**, backend **IL2CPP**,
metadatos **31.1**.

## Estado real

La recuperación local contiene recursos y estructura del proyecto. El repositorio
guarda nuestras herramientas, informes e instrucciones; los archivos del juego y
los recursos extraídos se transfieren o generan fuera de Git.
**Todavía no es una reconstrucción jugable equivalente al original.** Las clases C#
exportadas contienen métodos vacíos y valores por defecto. Una segunda extracción
con Cpp2IL `dll_il_recovery` produjo 18 309 cuerpos de método en
`Assembly-CSharp.dll`: todos son `ldnull; throw`. No se deben confundir estas DLL
con código de juego recuperado.

La equivalencia de comportamiento, la compilación y las partidas LAN necesitan
validación por separado. Cambiar el nombre del producto no reconstituye esa lógica.

## Archivos locales

- `Recovered/ExportedProject/`: proyecto exportado, escenas, imágenes, audio,
  animaciones, materiales, prefabs y scripts recuperados.
- `Original/`: copia sin modificar de la instalación utilizada; excluida de Git y transferida en un ZIP separado.
- `.local/il-recovery/`: ensamblados generados por la segunda extracción.
- `.local/native-analysis/`: análisis ISIL del código nativo para investigación.
- `.local/assetripper.log`: registro de extracción y sus errores.
- `.local/cpp2il-recovery.log` y `.local/cpp2il-native.log`: registros de código.
- `reports/il-analysis.json`: prueba cuantitativa de los métodos incompletos.
- `.local/recovery-audit.json`: inventario generado después de la exportación.
- `.local/finalization-status.json`: fase actual del proceso automático.
- `.local/finalization.log` y `.local/finalization-errors.log`: seguimiento del
  proceso que espera la exportación, cambia el nombre e intenta validar Unity.
- `.local/unity-validation.log`: resultado del editor, si pudo instalarse y arrancar.

Hay un proceso local de finalización que continúa mientras este equipo permanezca
encendido. Descarga e intenta instalar Unity 2022.3.44f1 dentro de `.local/Unity/`,
espera la finalización completa de AssetRipper, aplica el nombre del producto,
genera el inventario e intenta verificar la compilación en modo batch. Registra
los fallos; no los convierte en una declaración de éxito. No reconstruye
automáticamente los cuerpos de método ausentes.

Los contenidos originales conservan su procedencia; la licencia Apache del
repositorio no cambia la licencia de los recursos del juego ni de sus dependencias.

## Abrir y verificar

Usar Unity **2022.3.44f1** y abrir `Recovered/ExportedProject`. Conservar el registro
de la consola antes de editar los scripts. La exportación utiliza shaders de
sustitución: la fidelidad visual tampoco está garantizada.

El metadato `PlayerSettings.productName` se cambia con:

```powershell
python tools/rename_project.py
```

Este cambio afecta al nombre del producto de Unity. Los logotipos y textos
dibujados dentro de las escenas se conservan como evidencia del original.

## Repetir la recuperación

Herramientas oficiales utilizadas:
[AssetRipper 2.0.0](https://github.com/AssetRipper/AssetRipper/releases/tag/2.0.0)
y [Cpp2IL pre-release 21](https://github.com/SamboyCoding/Cpp2IL/releases/tag/2022.1.0-pre-release.21).
Los ejecutables descargados están en `.local/tools/`.

Iniciar AssetRipper en una terminal:

```powershell
& .local/tools/AssetRipper/AssetRipper.GUI.Free.exe --headless --port 57431 --log-path .local/assetripper.log
```

En otra terminal, indicar una carpeta de salida nueva para conservar la extracción
anterior:

```powershell
python tools/recover.py --source Original --out OtraRecuperacion
python tools/audit_recovery.py
```

Inspeccionar los ensamblados generados:

```powershell
python -m pip install --index-url https://pypi.org/simple dnfile dncil
python tools/inspect_il.py .local/il-recovery/Assembly-CSharp.dll
```

## Trabajo necesario para una versión equivalente

1. Reconstruir y verificar cuerpos de método a partir del binario nativo, incluidas
   corutinas, serialización, RPC, reglas de partida y dependencias.
2. Corregir errores de compilación de la exportación y recuperar shaders.
3. Resolver referencias de escenas, prefabs y Addressables.
4. Aplicar el cambio de nombre a la interfaz conservando el resto del contenido.
5. Construir y probar el cliente con al menos dos dispositivos en la misma red:
   descubrimiento, creación/unión, movimiento, tareas, asesinatos, reuniones,
   votación, final de partida y reconexiones.

La instalación de Steam permanece sin modificar. El historial destinado a GitHub
contiene solo herramientas, informes y documentación, sin archivos del juego ni
punteros LFS a ellos.

Para continuar en el MacBook, consultar [MIGRACION_MAC.md](MIGRACION_MAC.md).
El manifiesto `reports/original-manifest.json` permite verificar que todos los
archivos originales llegaron completos.

El paquete para Drive se genera con `python tools/package_original.py` y queda
en `.local/transfer/AmongUs-original-Windows.zip`, junto con su suma SHA-256.
