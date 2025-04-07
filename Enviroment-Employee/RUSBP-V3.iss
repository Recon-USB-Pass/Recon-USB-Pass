[Setup]
; Información básica del instalador
AppName=RUSBP-V3
AppVersion=1.0
DefaultDirName={commonpf}\RUSBP-V3
DefaultGroupName=RUSBP-V3
OutputDir=output
OutputBaseFilename=InstaladorRUSBP-V3
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
CreateUninstallRegKey=yes


[Files]
; Archivos a instalar
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Employee\Script.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Employee\Enviroment-Employee.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.vbs"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; Ejecutar el Script.bat para instalar el ambiente (con permisos de administrador)
Filename: "C:\Program Files (x86)\RUSBP-V3\Script.bat"; Parameters: "install"; StatusMsg: "Instalando configuraciones..."; Flags: runhidden runascurrentuser

[UninstallRun]
; Ejecutar el Script.bat para desinstalar el ambiente (con permisos de administrador)
Filename: "C:\Program Files (x86)\RUSBP-V3\Script.bat"; Parameters: "uninstall"; Flags: runhidden runascurrentuser; RunOnceId: "UninstallScriptRun"

[Registry]
; Para crear la entrada de desinstalación en el Panel de Control
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\RUSBP-V3"; ValueName: "DisplayName"; ValueData: "RUSBP-V3"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\RUSBP-V3"; ValueName: "UninstallString"; ValueData: "{uninstallexe}"; Flags: uninsdeletekey