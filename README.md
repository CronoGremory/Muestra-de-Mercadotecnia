# 🛒 Sistema Integral de Gestión - XXIV Muestra Mercadológica UAA

Plataforma web de alto rendimiento desarrollada para la Universidad Autónoma de Aguascalientes. Este sistema centraliza el registro de participantes, la recepción de archivos digitales, la evaluación dinámica por roles (Jueces/Docentes) y la automatización de notificaciones vía WhatsApp.

---

## Tabla de Contenidos
1. [Descripción Técnica](#-descripción-técnica)
2. [Arquitectura del Sistema](#-arquitectura-del-sistema)
3. [Requisitos Previos](#-requisitos-previos)
4. [Instalación y Despliegue (Docker)](#-instalación-y-despliegue-docker)
5. [Manual de Uso: Bot de WhatsApp](#-manual-de-uso-bot-de-whatsapp)
6. [Estructura del Proyecto](#-estructura-del-proyecto)

---

##  Descripción Técnica
El sistema resuelve la problemática de la gestión manual del evento mediante una solución digital que permite:
* **Gestión de Usuarios:** Roles diferenciados (Admin, Staff, Docente, Juez, Equipo) con seguridad BCrypt.
* **Evaluación en Tiempo Real:** Cálculo automático de promedios ponderados y detección de ganadores.
* **Notificaciones Inteligentes:** Integración con WhatsApp Web para recordatorios de fechas límite.
* **Persistencia Robusta:** Base de datos relacional normalizada en 3NF.

---

## Arquitectura del Sistema
El proyecto sigue un patrón de arquitectura **Cliente-Servidor Distribuida** y modular:

* **Backend:** ASP.NET Core 9.0 (C#) - API RESTful.
* **Frontend:** HTML5, CSS3, JavaScript Vainilla (SPA Pattern).
* **Base de Datos:** Oracle Database 21c (Contenedorizado).
* **Automatización:** Selenium WebDriver (Google Chrome).
* **Infraestructura:** Docker & Docker Compose.

---

## Requisitos Previos
Para ejecutar este proyecto, asegúrese de tener instalado:

1.  **Docker Desktop** (Configurado con WSL 2 en Windows).
2.  **Google Chrome** (Última versión, requerido para el bot de WhatsApp).
3.  **Git** (Para clonar el repositorio).

---

## Instalación y Despliegue (Docker)

Para desplegar el entorno completo (Aplicación + Base de Datos) de forma aislada:

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/tu-usuario/muestra-mercadologica.git](https://github.com/tu-usuario/muestra-mercadologica.git)
    cd muestra-mercadologica
    ```

2.  **Construir y levantar contenedores:**
    Abra una terminal en la raíz del proyecto y ejecute:
    ```bash
    docker-compose up --build
    ```
    *Espere a que la consola muestre que la base de datos y la app han iniciado.*

3.  **Acceder al Sistema:**
    * **Web App:** [http://localhost:5050/Modelos/index.html](http://localhost:5050/Modelos/index.html)
    * **Base de Datos:** Puerto `1521`.
    * **Credenciales Admin:** `AdrianaNoyola@uaa.mx` / `adriana12345`

---

## Manual de Uso: Bot de WhatsApp

El sistema incluye un módulo de automatización basado en Selenium que controla un navegador Chrome en el servidor para enviar mensajes de WhatsApp.

### Paso 1: Acceso al Panel
1. Inicie sesión como **Administrador**.
2. En el menú lateral, seleccione la opción **"Bot WhatsApp"** (o navegue a `/Modelos/Admin/admin_whatsapp.html`).

### Paso 2: Inicialización del Servicio
1. En la tarjeta "Estado del Servicio", haga clic en el botón **"Iniciar Bot / Abrir Chrome"**.
2. **¡Importante!** Se abrirá una ventana física de Google Chrome en el servidor (su computadora).
3. No cierre esta ventana. El sistema la necesita abierta para controlar WhatsApp Web.

### Paso 3: Vinculación (Escaneo de QR)
1. En la ventana de Chrome que se abrió, aparecerá el código QR de WhatsApp Web.
2. Abra WhatsApp en su teléfono móvil -> Menú (tres puntos) -> Dispositivos vinculados -> **Vincular un dispositivo**.
3. Escanee el código QR.
4. Espere a que carguen sus chats en la ventana del navegador del servidor.

### Paso 4: Envío de Notificaciones
Una vez vinculado, puede realizar dos acciones desde el panel web:

* **Prueba Unitaria:**
    * Ingrese un número de teléfono con código de país (Ej: `5214491234567`) en el campo de prueba.
    * Haga clic en "Enviar".
    * Verifique en su celular que el mensaje se haya enviado.

* **Envío Masivo:**
    * Haga clic en **"Ejecutar Envío Masivo"**.
    * El sistema consultará la base de datos Oracle, filtrará los usuarios con entregas pendientes y les enviará un recordatorio automáticamente uno por uno.
    * Podrá ver el progreso en la consola de logs del panel.

---

## Estructura del Proyecto

```text
Muestra/
├── Controllers/       # Controladores API (Backend Logic)
│   ├── AdminController.cs
│   ├── WhatsApiController.cs  <-- Lógica del Bot
│   └── ...
├── Modelos/           # Vistas HTML organizadas por Rol
│   ├── Admin/         # Vistas protegidas de administrador
│   ├── Equipo/        # Vistas para alumnos
│   └── ...
├── Estilos/           # CSS personalizado (styleflujos.css)
├── Dockerfile         # Definición de imagen del contenedor App
└── docker-compose.yml # Orquestación de servicios (App + Oracle)