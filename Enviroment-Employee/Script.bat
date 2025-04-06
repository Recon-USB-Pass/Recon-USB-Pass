@echo off
:: Habilitar auditpol o deshabilitar segÃºn el parámetro

net session >nul 2>$1 || (powershell Start-Process "%~f0" -Verb runAs & exit /b)

chcp 65001 >nul

if "%1"=="install" (
    echo Instalando configuraciones

    :: Habilitar auditpol para Logon/Logoff
    echo Habilitando auditpol para Logon/Logoff
    auditpol /set /subcategory:"Inicio de sesión" /success:enable /failure:enable
    auditpol /set /subcategory:"Cerrar sesión" /success:enable /failure:enable
    auditpol /set /subcategory:"Bloqueo de cuenta" /success:enable /failure:enable
    auditpol /set /subcategory:"Modo principal de IPsec" /success:enable /failure:enable
    auditpol /set /subcategory:"Modo rápido de IPsec" /success:enable /failure:enable
    auditpol /set /subcategory:"Modo extendido de IPsec" /success:enable /failure:enable
    auditpol /set /subcategory:"Inicio de sesión especial" /success:enable /failure:enable
    auditpol /set /subcategory:"Otros eventos de inicio y cierre de sesión" /success:enable /failure:enable
    auditpol /set /subcategory:"Servidor de directivas de redes" /success:enable /failure:enable
    auditpol /set /subcategory:"Notificaciones de usuario o dispositivo" /success:enable /failure:enable
    auditpol /set /subcategory:"Pertenencia a grupos" /success:enable /failure:enable
    echo Auditpol habilitado.

    echo  Create Task & Trigger

    schtasks /create /tn "Enviroment-Employee" /xml "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Employee\Enviroment-Employee.xml" /f

    echo Instalación completada con Éxito.
)


if "%1"=="uninstall" (
    echo Desinstalando configuraciones

    :: Eliminar la tarea programada
    echo Eliminando la tarea programada "Enviroment-Employee"
    schtasks /delete /tn "Enviroment-Employee" /f
    echo Tarea programada eliminada.

    :: Deshabilitar los permisos de auditpol
    echo Deshabilitando auditpol
    auditpol /set /subcategory:"Inicio de sesión" /success:disable /failure:disable
    echo Inicio de sesión Borrado
    auditpol /set /subcategory:"Cerrar sesión" /success:disable /failure:disable
    echo Cerrar sesión Borrado
    auditpol /set /subcategory:"Bloqueo de cuenta" /success:disable /failure:disable
    echo Bloqueo de cuenta Borrado
    auditpol /set /subcategory:"Modo principal de IPsec" /success:disable /failure:disable
    echo Modo principal de IPsec Borrado
    auditpol /set /subcategory:"Modo rápido de IPsec" /success:disable /failure:disable
    echo Modo rápido de IPsec Borrado
    auditpol /set /subcategory:"Modo extendido de IPsec" /success:disable /failure:disable
    echo Modo extendido de IPsec Borrado
    auditpol /set /subcategory:"Inicio de sesión especial" /success:disable /failure:disable
    echo Inicio de sesión especial Borrado
    auditpol /set /subcategory:"Otros eventos de inicio y cierre de sesión" /success:disable /failure:disable
    echo Otros eventos de inicio y cierre de sesión Borrado
    auditpol /set /subcategory:"Servidor de directivas de redes" /success:disable /failure:disable
    echo Servidor de directivas de redes Borrado
    auditpol /set /subcategory:"Notificaciones de usuario o dispositivo" /success:disable /failure:disable
    echo Notificaciones de usuario o dispositivo Borrado
    auditpol /set /subcategory:"Pertenencia a grupos" /success:disable /failure:disable
    echo Pertenencia a grupos Borrado
    echo Auditpol deshabilitado.

    echo Desinstalación completada con Exito.
)
::C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Employee
:: Si no se pasa el parámetro install o uninstall, mostrar mensaje de uso
if NOT "%1"=="install" if NOT "%1"=="uninstall" (
    echo Uso: %0 install o %0 uninstall
    echo Ejemplo para instalar: %0 install
    echo Ejemplo para desinstalar: %0 uninstall
)
pause
exit