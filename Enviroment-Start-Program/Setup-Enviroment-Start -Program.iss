[Setup]
AppName=Mi AplicaciÃ³n
AppVersion=1.0
DefaultDirName={commonpf}\MiAplicacion
DefaultGroupName=MiAplicacion
OutputDir=output
OutputBaseFilename=MiAplicacion_Installer
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes

[Files]
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\crear_trigger.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\Script.bat"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\instalador.bat"; Parameters: ""; StatusMsg: "Instalando el programa..."; Flags: runhidden

[UninstallRun]
Filename: "{app}\instalador.bat"; Parameters: "desinstalar"; Flags: runhidden; RunOnceId: "desinstalar"