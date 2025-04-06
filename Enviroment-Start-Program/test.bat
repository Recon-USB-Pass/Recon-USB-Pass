@echo off}
echo Hola mundo
::ping 127.0.0.1 -n 6 >nul
::echo Adios
::ping 127.0.0.1 -n 4 >nul
set ruta_vbs=C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.vbs
cscript //nologo "%ruta_vbs%"
exit
