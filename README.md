# Recon-USB-Pass
Repositorio principal del proyecto. Contiene la información general del proyecto, creacion de organizacion repositorios, creacion de historias e issues, incluyendo la descripción del proyecto, objetivos, alcance y documentación inicial. También incluye el software principal versionado de autenticación multifactorial basada en tokens USB y PINs.

## **Por qué Python es la Mejor Elección**

### 1. **Economía: Gratuito y de Código Abierto**
   - **Razón:** Python es completamente gratuito y no requiere licencias ni costos adicionales.
   - **Comparación con otros lenguajes:**
     - **C#:** Aunque Visual Studio Community es gratuito, C# está más orientado al ecosistema de Microsoft, lo que puede implicar dependencias externas o herramientas premium en el futuro.
     - **Java:** Java es gratuito, pero configurar entornos y herramientas como Maven/Gradle puede ser más complejo.
     - **Go/Rust:** Estos lenguajes son modernos y potentes, pero tienen una curva de aprendizaje más pronunciada y menos bibliotecas preconstruidas para tareas específicas como interacción con USB o cifrado.

---

### 2. **Simplicidad: Fácil de Aprender y Usar**
   - **Razón:** Python tiene una sintaxis clara y legible, lo que lo hace ideal para proyectos rápidos y prototipos.
   - **Comparación con otros lenguajes:**
     - **C++:** Aunque es extremadamente poderoso, su sintaxis es mucho más compleja y propensa a errores.
     - **Bash/PowerShell:** Estos son útiles para scripting simple, pero no son adecuados para aplicaciones completas con interfaces gráficas y comunicación entre componentes.
     - **JavaScript:** Aunque es versátil, JavaScript está más orientado a desarrollo web y no es tan intuitivo para tareas del sistema operativo como detectar dispositivos USB.

---

### 3. **Compatibilidad con Windows: Ideal para Interactuar con el Sistema Operativo**
   - **Razón:** Python tiene bibliotecas específicas para interactuar con funciones de Windows, como bloquear el teclado/mouse, detectar dispositivos USB y automatizar tareas.
   - **Comparación con otros lenguajes:**
     - **C#:** Es nativo de Windows y muy potente, pero requiere aprender un nuevo ecosistema (.NET) y puede depender de herramientas como Visual Studio.
     - **Bash:** No es compatible nativamente con Windows sin usar WSL (Windows Subsystem for Linux).
     - **Go:** Aunque es multiplataforma, Go no tiene tantas bibliotecas especializadas para interactuar directamente con hardware o funciones específicas de Windows.

---

### 4. **Bibliotecas y Herramientas Preconstruidas: Todo lo Necesario Ya Existe**
   - **Razón:** Python tiene una amplia gama de bibliotecas gratuitas y bien documentadas para cubrir todas las necesidades del proyecto:
     - **Interacción con USB:** `pyudev`, `pywin32`, `wmi`.
     - **Cifrado:** `cryptography`, `pycryptodome`.
     - **Interfaces gráficas:** `Tkinter`, `PyQt`, `Kivy`.
     - **Backend:** `Flask`, `FastAPI`.
     - **Base de datos:** `psycopg2` para PostgreSQL, `sqlite3` para SQLite.
     - **Pruebas:** `pytest`, `unittest`.
     - **Empaquetado:** `PyInstaller` para crear ejecutables.
   - **Comparación con otros lenguajes:**
     - **C#:** Aunque tiene bibliotecas robustas, muchas de ellas están limitadas al ecosistema de Microsoft.
     - **Java:** Requiere configurar herramientas adicionales (Maven/Gradle) y carece de bibliotecas específicas para interacción con USB.
     - **Go:** Carece de bibliotecas maduras para GUIs y algunas tareas específicas como detección de USB.

---

### 5. **Desarrollo Rápido: Prototipos en Pocas Horas**
   - **Razón:** Python permite desarrollar rápidamente prototipos funcionales debido a su simplicidad y gran cantidad de bibliotecas preconstruidas.
   - **Comparación con otros lenguajes:**
     - **C++:** Requiere más tiempo para escribir código debido a su sintaxis compleja y falta de abstracciones.
     - **Rust:** Aunque es seguro y moderno, Rust tiene una curva de aprendizaje empinada y es más adecuado para proyectos a largo plazo.
     - **PHP:** Aunque es gratuito, PHP está más orientado a desarrollo web y no es ideal para aplicaciones de escritorio.

---

### 6. **Escalabilidad: De Prototipo a Producto Final**
   - **Razón:** Python es suficientemente flexible para escalar desde un prototipo simple hasta un producto final robusto. Puedes agregar nuevas funcionalidades gradualmente sin necesidad de reescribir el código base.
   - **Comparación con otros lenguajes:**
     - **Bash/PowerShell:** No son adecuados para proyectos grandes debido a su naturaleza limitada.
     - **C/C++:** Aunque son escalables, requieren más tiempo y recursos para desarrollar y mantener.
     - **JavaScript:** Aunque es escalable, JavaScript está más orientado a desarrollo web y no es ideal para aplicaciones de escritorio.

---

### 7. **Comunidad y Soporte: Documentación y Recursos Gratuitos**
   - **Razón:** Python tiene una comunidad enorme y activa, lo que significa que encontrarás tutoriales, ejemplos y soporte gratuito para resolver problemas.
   - **Comparación con otros lenguajes:**
     - **Rust:** Aunque tiene una comunidad creciente, es mucho más pequeña que la de Python.
     - **Go:** Tiene una comunidad sólida, pero menos recursos específicos para tareas como interacción con USB o GUIs.
     - **Perl:** Aunque es potente, Perl tiene una comunidad en declive y menos recursos disponibles.

---

### 8. **Compatibilidad con Docker y GitHub Actions**
   - **Razón:** Python es compatible con Docker y GitHub Actions, lo que facilita la automatización del despliegue y las pruebas.
   - **Comparación con otros lenguajes:**
     - **C#:** Compatible con Docker, pero requiere más configuración debido a su dependencia de .NET.
     - **Bash:** No es ideal para integrarse con Docker o GitHub Actions en proyectos complejos.
     - **Go:** Compatible, pero requiere más configuración manual.

---

### 9. **Multiplataforma: Funciona en Windows, Linux y macOS**
   - **Razón:** Aunque tu proyecto está enfocado en Windows, Python es multiplataforma, lo que significa que podrías adaptarlo fácilmente a otros sistemas operativos en el futuro si fuera necesario.
   - **Comparación con otros lenguajes:**
     - **C#:** Menos flexible fuera del ecosistema de Windows.
     - **Bash:** Limitado a Linux/macOS sin usar WSL.
     - **Go:** Multiplataforma, pero menos maduro para GUIs.

---

### Resumen Comparativo

| Característica                | Python                     | C#                         | Java                       | Go                         | Bash/PowerShell            |
|-------------------------------|----------------------------|----------------------------|----------------------------|----------------------------|----------------------------|
| **Costo**                     | Gratuito                   | Gratuito (Visual Studio)    | Gratuito                   | Gratuito                   | Gratuito                   |
| **Curva de aprendizaje**       | Baja                       | Media                      | Media                      | Alta                       | Baja (limitado)            |
| **Compatibilidad con Windows** | Excelente                  | Nativo                     | Buena                      | Buena                      | Nativa (limitada)          |
| **Interacción con USB**        | Bibliotecas específicas    | Depende de .NET            | Limitado                   | Limitado                   | Muy limitado               |
| **Cifrado y seguridad**        | Bibliotecas maduras        | Robusto (.NET)             | Buenas bibliotecas         | En desarrollo              | Limitado                   |
| **Interfaces gráficas**        | Tkinter, PyQt, Kivy        | Windows Forms, WPF         | Swing, JavaFX              | Limitado                   | No aplica                  |
| **Backend/APIs**              | Flask, FastAPI             | ASP.NET Core               | Spring Boot                | Built-in HTTP server       | No aplica                  |
| **Pruebas y CI/CD**            | Pytest, GitHub Actions     | NUnit, GitHub Actions      | JUnit, GitHub Actions      | Testing frameworks         | Scripts simples            |

---

# Flujo del Proyecto del Sistema de Autenticación Multifactorial con Tokens USB**

## **Introducción**
Aqui se describe el flujo completo del sistema de autenticación multifactorial basado en tokens USB para la Municipalidad de Valparaíso. El objetivo es mejorar la seguridad del acceso a los equipos municipales mediante un proceso robusto que incluye configuración inicial por parte del administrador, validación de tokens, registro de actividades y cifrado de datos. A continuación, se detalla el flujo de trabajo, las tecnologías utilizadas y los desafíos técnicos a resolver.

---

## **1. Configuración Inicial (Realizada por el Administrador)**

### **Paso 1: Asignación del Token USB al Empleado**
- **Descripción:**  
  El administrador asigna un token USB único a cada empleado. Este token contiene un número de serie específico y está asociado con una clave PIN única generada para el usuario. El administrador registra estos datos en el sistema para futuras validaciones.
- **Tecnología:**  
  - Base de datos PostgreSQL para almacenar los números de serie autorizados y sus respectivos PINs.
  - Interfaz web (Tkinter/PyQt o HTML/CSS/JavaScript) para facilitar la gestión de tokens y usuarios.

### **Paso 2: Configuración del PC del Empleado**
- **Descripción:**  
  El administrador configura el PC del empleado instalando el **software demonio**. Este software se activará automáticamente después del inicio de sesión estándar y bloqueará el sistema hasta que el empleado complete el proceso de autenticación multifactorial.
- **Tecnología:**  
  - PyInstaller para crear un instalador ejecutable (.exe) del software demonio.
  - Python (`pywin32` o `ctypes`) para interactuar con funciones nativas de Windows (bloqueo del sistema operativo).

---

## **2. Proceso de Autenticación (Realizado por el Empleado)**

### **Paso 1: Acceso con Contraseña Común**
- **Descripción:**  
  El empleado inicia sesión en el PC con una contraseña común preconfigurada (por ejemplo, "muni"). Esta contraseña permite acceder al sistema operativo, pero el software demonio se activa inmediatamente después.
- **Tecnología:**  
  - Windows Login (configuración inicial estándar).
  - Software demonio desarrollado en Python.

### **Paso 2: Bloqueo del Sistema Operativo**
- **Descripción:**  
  Una vez que el empleado inicia sesión con la contraseña común, el software demonio bloquea inmediatamente el sistema operativo. El empleado no puede realizar ninguna acción hasta completar el proceso de autenticación multifactorial.
- **Tecnología:**  
  - Tkinter o PyQt para mostrar una interfaz gráfica simple que solicite el token USB y el PIN.
  - Python (`pywin32`) para bloquear el teclado/mouse y evitar cualquier interacción no autorizada.

### **Paso 3: Conexión del Token USB**
- **Descripción:**  
  El sistema solicita al empleado conectar su token USB autorizado. El software demonio detecta automáticamente el dispositivo USB y lee su número de serie.
- **Tecnología:**  
  - Bibliotecas como `pyudev` o `pywin32` para detectar dispositivos USB y leer su número de serie.
  - Validación del número de serie para evitar clonaciones (verificar integridad mediante PKI).

### **Paso 4: Ingreso del PIN**
- **Descripción:**  
  Una vez conectado el token USB, el sistema solicita al empleado ingresar el PIN asociado al token. Este PIN es único para cada usuario.
- **Tecnología:**  
  - Tkinter o PyQt para crear un campo de entrada seguro (ocultar caracteres ingresados).

### **Paso 5: Validación en el Backend**
- **Descripción:**  
  El software demonio envía los siguientes datos al backend para su validación:
  - Número de serie del token USB (cifrado con PKI).
  - PIN ingresado por el usuario (cifrado con PKI).
  - Información adicional del PC (MAC address, IP, timestamp).
- **Tecnología:**  
  - Backend desarrollado en Flask o FastAPI para recibir y validar los datos.
  - Base de datos PostgreSQL para almacenar tokens autorizados y logs.
  - Cifrado AES o RSA usando la biblioteca `cryptography` para proteger los datos.

### **Paso 6: Desbloqueo o Denegación de Acceso**
- **Descripción:**  
  Si la validación es exitosa, el backend responde con un mensaje de éxito y el software demonio desbloquea el sistema operativo. Si falla, el sistema muestra un mensaje de error y vuelve a solicitar los datos.
- **Tecnología:**  
  - Respuesta JSON desde el backend.
  - Control de bloqueo/desbloqueo mediante Python (`pywin32`).

---

## **3. Registro de Actividades**

### **Registro de Datos al Inicio de Sesión**
- **Descripción:**  
  Una vez que el empleado accede al sistema, el software demonio registra los siguientes datos:
  - MAC address del PC.
  - Dirección IP del PC.
  - Número de serie del token USB.
  - Hora global de inicio de sesión.
- **Tecnología:**  
  - Logs almacenados en PostgreSQL.
  - Uso de PKI para cifrar los datos antes de almacenarlos.

### **Registro de Datos al Cierre de Sesión**
- **Descripción:**  
  Cuando el empleado cierra sesión o apaga el PC, el software demonio registra los siguientes datos:
  - Hora global de cierre de sesión.
  - Actividades realizadas durante la sesión (opcional, más adelante).
- **Tecnología:**  
  - Eventos de Windows (`pywin32`) para detectar cierre de sesión.
  - Actualización de logs en PostgreSQL.

---

## **4. Gestión de Tokens y Usuarios (Vista de Administrador)**

### **Asignación de Tokens**
- **Descripción:**  
  Un administrador asigna tokens USB a los empleados desde una interfaz web. Esta interfaz permite:
  - Agregar nuevos tokens autorizados.
  - Asociar tokens con usuarios específicos.
  - Ver logs de actividad.
- **Tecnología:**  
  - Frontend desarrollado en Tkinter/PyQt o HTML/CSS/JavaScript (Flask/FastAPI + Jinja2).
  - Backend conectado a PostgreSQL para gestionar tokens y usuarios.

### **Protección contra Clonaciones**
- **Descripción:**  
  Para evitar clonaciones de tokens, el sistema valida el número de serie del token USB y lo asocia con un certificado digital único generado mediante PKI.
- **Tecnología:**  
  - OpenSSL para generar certificados digitales.
  - Almacenamiento seguro de certificados en PostgreSQL.

---

## **5. Tecnologías Utilizadas**

### **Backend**
- **Framework:** Flask o FastAPI.
- **Base de Datos:** PostgreSQL (con cifrado `pgcrypto`).
- **Cifrado:** `cryptography` (AES/RSA), OpenSSL (PKI).

### **Frontend**
- **Interfaz Local (Software Demonio):** Tkinter/PyQt.
- **Interfaz Web (Administrador):** HTML/CSS/JavaScript (opcionalmente React.js o Vue.js si se requiere algo más avanzado).

### **Infraestructura**
- **Empaquetado:** PyInstaller para crear instaladores ejecutables (.exe).
- **Seguridad:** Certificados SSL/TLS para cifrado en tránsito.
- **Logs:** PostgreSQL para almacenamiento centralizado.

---

## **6. Desafíos Técnicos**

1. **Bloqueo del Sistema Operativo:**
   - Investigar cómo usar `pywin32` o `ctypes` para bloquear el teclado/mouse en Windows.
2. **Detección de Tokens USB:**
   - Usar bibliotecas como `pyudev` o `pywin32` para leer el número de serie del token.
3. **Generación y Almacenamiento de Certificados PKI:**
   - Implementar OpenSSL para generar certificados únicos por token.
4. **Clonación de Tokens:**
   - Validar el número de serie del token y asociarlo con un certificado digital único.
5. **Instaladores para PCs Nuevos:**
   - Usar PyInstaller para crear instaladores ejecutables que instalen el software demonio.
6. **Registro de Actividades Sospechosas:**
   - Futura implementación para analizar comportamientos sospechosos (ejemplo: intentos fallidos repetidos).

---

## **Conclusión**
Este proyecto implementa un sistema de autenticación multifactorial robusto y seguro, diseñado específicamente para la Municipalidad de Valparaíso. Al combinar tokens USB, cifrado PKI y registro de actividades, garantiza que solo usuarios autorizados puedan acceder a los equipos municipales. Las tecnologías seleccionadas (Python, Flask, PostgreSQL, OpenSSL) permiten un desarrollo eficiente y escalable, mientras que los desafíos técnicos identificados serán abordados durante la implementación.