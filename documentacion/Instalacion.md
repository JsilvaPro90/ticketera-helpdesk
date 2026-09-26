# Guía de Instalación - Ticketera Helpdesk

## Requisitos previos

Antes de ejecutar el proyecto se requiere:

- Visual Studio 2019 o superior.
- .NET Framework compatible con el proyecto.
- SQL Server.
- SQL Server Management Studio.
- Git.

---

# Configuración del proyecto

## 1. Clonar repositorio

Ejecutar:

git clone https://github.com/JsilvaPro90/ticketera-helpdesk.git


## 2. Abrir solución

Abrir el archivo:

Ticketera.sln

desde Visual Studio.

---

## 3. Configurar base de datos

Crear la base de datos en SQL Server.

Actualizar la cadena de conexión ubicada en:

Web.config

con los datos del servidor SQL correspondiente.

---

## 4. Restaurar paquetes

Desde Visual Studio:

Tools
→ NuGet Package Manager
→ Restore NuGet Packages

---

## 5. Ejecutar aplicación

Seleccionar el proyecto web como inicio.

Ejecutar mediante:

IIS Express

o servidor local configurado.

---

# Estructura principal

Ticketera
│
├── Controllers
├── Models
├── Views
├── Services
└── Content

---

# Tecnologías

- C#
- ASP.NET MVC
- Entity Framework
- SQL Server
- HTML
- CSS
- JavaScript