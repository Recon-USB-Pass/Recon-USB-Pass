@echo off}
echo Hola mundo
::ping 127.0.0.1 -n 6 >nul
::echo Adios
::ping 127.0.0.1 -n 4 >nul
set ruta_vbs=C:\Program Files (x86)\RUSBP-V3\test.vbs
cscript //nologo "%ruta_vbs%"
exit
