# Continuar en el MacBook

La instalación original de Windows está en `Original/` (105 archivos, unos 1,15 GB).
Contiene `GameAssembly.dll`, los metadatos de IL2CPP, escenas y paquetes de recursos.
No hace falta instalar Steam para analizar estos archivos. El ejecutable de Windows
no se convierte en una aplicación de macOS al copiarlo.

El cambio de ordenador es viable. No recupera por sí mismo los cuerpos de método
que faltan ni garantiza que el trabajo avance más rápido. La reconstrucción de la
lógica y las pruebas LAN siguen pendientes.

## 1. Clonar nuestro trabajo

El repositorio contiene herramientas, informes e instrucciones; los archivos del
juego se transfieren por separado. Usar Python 3.11 o posterior. No hace falta Git LFS.

```sh
git clone https://github.com/Ander-tsx/AmongUs-decomp.git
cd AmongUs-decomp
```

## 2. Descargar el juego desde tu Drive

Subir `AmongUs-original-Windows.zip` y su archivo `.sha256` al Drive desde Windows.
Descargarlos en el Mac. El ZIP contiene únicamente 105 archivos originales bajo
la carpeta `Original/`; las herramientas y los informes vienen del repositorio.

Desde la raíz del repositorio clonado, adaptar la ruta si se descargó en otra carpeta:

```sh
cd ~/Downloads
shasum -a 256 -c AmongUs-original-Windows.zip.sha256
cd /ruta/al/AmongUs-decomp
unzip ~/Downloads/AmongUs-original-Windows.zip -d .
python3 tools/verify_original.py
```

La comprobación debe indicar que coinciden los hashes SHA-256 de los 105 archivos.
La carpeta `Original/` queda ignorada por Git. No subir el ZIP al repositorio.

## 3. Herramientas nativas de macOS

```sh
python3 tools/setup_macos.py
```

El script descarga las versiones utilizadas de AssetRipper y Cpp2IL desde sus
repositorios oficiales y elige Apple Silicon o Intel según el Mac. No descarga
el juego ni necesita Steam. La ejecución de estas herramientas en ese Mac aún
debe verificarse allí.

Iniciar AssetRipper con el ejecutable que indique el script:

```sh
.local/tools/AssetRipper/AssetRipper.GUI.Free --headless --port 57431 --log-path .local/assetripper.log
```

En otra terminal, exportar a una carpeta nueva:

```sh
python3 tools/recover.py --source Original --out RecoveredMac
python3 tools/rename_project.py --project RecoveredMac/ExportedProject
python3 tools/audit_recovery.py --project RecoveredMac/ExportedProject
```

Para regenerar el análisis nativo en el Mac:

```sh
.local/tools/Cpp2IL --game-path Original --exe-name "Among Us" --output-as isil --output-to .local/native-analysis
```

Instalar Unity 2022.3.44f1 para macOS desde Unity Hub o el archivo oficial de
versiones. Usar la edición apropiada para Apple Silicon o Intel. Abrir
`RecoveredMac/ExportedProject` y revisar los errores de consola. El proyecto
recuperado no está confirmado como compilable ni jugable.

## Crear de nuevo el ZIP en Windows

```powershell
python tools/package_original.py
```

El script genera `.local/transfer/AmongUs-original-Windows.zip`, verifica todos
los originales comprimidos y escribe el archivo `.zip.sha256`. No incluye
herramientas, informes, análisis nativo, instaladores ni la exportación de Unity.

El proceso `tools/finalize_recovery.py` es específico de Windows. La extracción
que sigue ejecutándose en el PC y sus procesos no se trasladan al Mac al clonar.
