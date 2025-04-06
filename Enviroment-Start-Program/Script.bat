@echo off
setlocal enabledelayedexpansion

:: Definir variables
set tareaNombre=BloqueoDePantalla
set ejecutableRuta=C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.bat
set desinstalar=false

:: Comprobar si el primer parÃ¡metro es "desinstalar"
if "%1"=="desinstalar" set desinstalar=true

:: OpciÃ³n para desinstalar
if "%desinstalar%"=="true" (
    echo Desinstalando el programa...
    
    :: Eliminar la tarea programada y los triggers
    echo Eliminando tarea programada y triggers asociados...
    schtasks /delete /tn "%tareaNombre%" /f
    
    echo Tarea programada eliminada.
    echo DesinstalaciÃ³n completa.
    exit /b
)

:: Instalador - Crear la tarea programada
echo Instalando el programa...

:: Comprobar si la tarea ya existe
schtasks /query /tn "%tareaNombre%" >nul 2>nul
if %errorlevel%==0 (
    echo La tarea ya existe. Si desea actualizarla, elimÃ­nela primero.
    exit /b
)

:: Crear la tarea programada para ejecutarse al iniciar sesiÃ³n
echo Creando tarea programada para ejecutarse al iniciar sesiÃ³n...
schtasks /create /tn "%tareaNombre%" /tr "%ejecutableRuta%" /sc onlogon /ru "SYSTEM" /RL HIGHEST /f

:: Crear el trigger de desbloqueo de pantalla (usando PowerShell)
echo Creando trigger para desbloqueo de pantalla...
powershell -ExecutionPolicy Bypass -File "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\crear_trigger.ps1"

echo InstalaciÃ³n completada.
pause