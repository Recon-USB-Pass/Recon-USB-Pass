$taskName = "BloqueoDePantalla"
$ejecutableRuta = "C:\Users\RUSBP\Desktop\Recon-USB-Pass\Recon-USB-Pass\Enviroment-Start-Program\test.bat"

# Comprobar si la tarea ya existe
$taskExist = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($taskExist) {
    Write-Host "La tarea '$taskName' ya existe. Configurando el trigger..."

    # Crear el trigger de evento de inicio de sesiÃ³n (evento 4624)
    $triggerEvent = New-ScheduledTaskTrigger -AtStartup
    $triggerEvent.EventTrigger = 4624 # Este es el cÃ³digo de evento para inicio de sesiÃ³n (Logon)

    # Crear la acciÃ³n (ejecutar el archivo .exe)
    $taskAction = New-ScheduledTaskAction -Execute $ejecutableRuta

    # Crear la configuraciÃ³n de la tarea
    $taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -StartWhenAvailable

    # Registrar el trigger y actualizar la tarea
    Set-ScheduledTask -TaskName $taskName -Action $taskAction -Settings $taskSettings -Trigger $triggerEvent
    Write-Host "Trigger actualizado correctamente."
} else {
    Write-Host "La tarea '$taskName' no existe. AsegÃºrese de que la tarea estÃ© registrada antes de ejecutar este script."
}