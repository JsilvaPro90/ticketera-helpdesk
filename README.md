# Ticketera · Sistema de gestión de tickets para soporte TI

Proyecto académico desarrollado para centralizar y organizar la gestión de incidencias de soporte mediante tickets trazables.

## Descripción

Ticketera es un sistema web de Mesa de Servicio (Help Desk) que permite registrar y dar seguimiento a incidencias, organizar empresas y contactos, administrar usuarios y roles, y visualizar indicadores operativos desde un dashboard.

El proyecto busca transformar solicitudes de soporte dispersas en un flujo de atención más ordenado y visible, donde cada incidencia puede clasificarse por urgencia, asignarse al personal de soporte y seguirse durante su ciclo de atención.

## Funcionalidades principales

- Autenticación de usuarios.
- Gestión de tickets de soporte.
- Clasificación por estado y nivel de urgencia.
- Administración de empresas y contactos.
- Gestión de usuarios y roles.
- Dashboard con indicadores operativos.
- Adjuntos asociados a tickets.
- Notificaciones por correo electrónico.
- Recuperación y cambio de contraseña.
- Importación masiva de empresas y contactos desde Excel.

## Tecnologías

- C#
- ASP.NET MVC 5
- .NET Framework 4.8
- Entity Framework 6
- SQL Server / LocalDB
- HTML, CSS y JavaScript
- ClosedXML para procesamiento de Excel
- Visual Studio

## Arquitectura general

La solución sigue el patrón MVC:

- **Models:** entidades, ViewModels y acceso a datos.
- **Views:** interfaz de usuario.
- **Controllers:** flujo de las solicitudes y lógica de aplicación.
- **Services:** funcionalidades complementarias, como correo y notificaciones.
- **SQL Server:** persistencia de la información mediante Entity Framework.

## Configuración local

1. Clona el repositorio.
2. Abre `Ticketera/Ticketera.sln` en Visual Studio.
3. Restaura los paquetes NuGet definidos en `packages.config`.
4. Revisa la cadena `DefaultConnection` de `Ticketera/Web.config`.
5. Configura los valores SMTP de ejemplo si deseas probar el envío de correos.
6. Ejecuta las migraciones de Entity Framework si corresponde.
7. Inicia el proyecto desde Visual Studio.

### Configuración SMTP

El repositorio **no incluye credenciales reales**. En `Ticketera/Web.config` encontrarás valores de ejemplo como:

```xml
<add key="SmtpUser" value="YOUR_EMAIL@gmail.com" />
<add key="SmtpPass" value="YOUR_GMAIL_APP_PASSWORD" />
```

Sustitúyelos únicamente en tu entorno local y nunca publiques contraseñas o App Passwords reales.

## Seguridad del repositorio

Se excluyeron del repositorio archivos generados por Visual Studio, binarios, paquetes restaurables, archivos locales, adjuntos de pruebas y configuraciones sensibles.

## Estado

Proyecto académico y de aprendizaje. El objetivo principal es practicar desarrollo web con C#, ASP.NET MVC, Entity Framework, SQL Server e integración con archivos Excel.

## Autor

**José Gustavo Silva Medrano**  
Estudiante de Ingeniería de Sistemas · Lima, Perú
