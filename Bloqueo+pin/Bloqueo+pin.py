import ctypes
import time
from tkinter import Tk, Label, Entry, Button

# Bloquear el teclado y el ratÃ³n
def disable_input():
    ctypes.windll.user32.BlockInput(True)  # Bloquea el teclado y el ratÃ³n

# Desbloquear el teclado y el ratÃ³n
def enable_input():
    ctypes.windll.user32.BlockInput(False)  # Habilita el teclado y el ratÃ³n

# Crear una ventana de autenticaciÃ³n con tkinter
def show_authentication_window():
    root = Tk()
    root.title("AutenticaciÃ³n Requerida")

    # Crear una etiqueta y cuadro de texto para ingresar el PIN
    label = Label(root, text="Ingrese el PIN:")
    label.pack(padx=10, pady=10)

    password_entry = Entry(root, show="*")  # Entrada oculta para el PIN
    password_entry.pack(padx=10, pady=10)

    # FunciÃ³n para validar el PIN
    def validate_password():
        if password_entry.get() == "1234":  # PIN vÃ¡lido
            enable_input()  # Desbloquear el acceso
            root.quit()  # Cerrar la ventana de autenticaciÃ³n
            print("Acceso concedido")
        else:
            label.config(text="PIN incorrecto. Intente nuevamente.")
            password_entry.delete(0, 'end')  # Borrar la entrada del PIN

    # BotÃ³n para enviar el PIN
    button = Button(root, text="Aceptar", command=validate_password)
    button.pack(pady=10)

    # Mostrar la ventana
    root.mainloop()

def main():
    disable_input()  # Bloquear acceso inicialmente

    # Mostrar la ventana de autenticaciÃ³n
    show_authentication_window()  # Esta ventana permite al usuario ingresar el PIN

if __name__ == "__main__":
    main()