[Setup]
AppName=RUSBP-V1
AppVersion=1.0
DefaultDirName={commonpf}\RUSBP-V1
DefaultGroupName=RUSBP-V1
OutputDir=RUSBP-Vx-Inno
OutputBaseFilename=RUSBP-V1-Installer
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes

[Files]
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Ejecutable\dist\hola_mundo.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\RUSBP-V1"; Filename: "{app}\hola_mundo.exe"

[Registry]
;aqui van las entradas al regiustro si es que los ocuparemos

[Run]
Filename: "{app}\hola_mundo.exe"; Description: "Ejecutar RUSBP-V1"; Flags: nowait postinstall skipifsilent

