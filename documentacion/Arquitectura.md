# Arquitectura del Sistema - Ticketera Helpdesk

## Descripción

Ticketera Helpdesk es una aplicación web desarrollada bajo el patrón arquitectónico MVC (Model View Controller), permitiendo separar la lógica de negocio, presentación y acceso a datos.

## Patrón utilizado

### Model
Responsable de representar las entidades del sistema:

- Usuario
- Ticket
- Entidad
- Contacto
- TicketAdjunto

### View
Contiene las interfaces visuales utilizadas por los usuarios del sistema.

### Controller
Gestiona las solicitudes del usuario, ejecuta la lógica correspondiente y comunica los modelos con las vistas.

## Flujo general

Usuario
↓
Controller
↓
Model / Entity Framework
↓
SQL Server
↓
Respuesta al usuario

## Tecnologías

- C#
- ASP.NET MVC
- Entity Framework
- SQL Server
- HTML
- CSS
- JavaScript
- GitHub