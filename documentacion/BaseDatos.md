# Modelo de Base de Datos - Ticketera Helpdesk

## Descripción

El sistema utiliza SQL Server como motor de base de datos y Entity Framework para la gestión de persistencia de información.

La estructura está diseñada para administrar usuarios, entidades, contactos y tickets de soporte.

---

## Entidades principales

### Usuario

Almacena la información de los usuarios que interactúan con el sistema.

Datos principales:

- Nombre de usuario
- Credenciales de acceso
- Estado del usuario
- Información personal

---

### Ticket

Entidad principal del sistema.

Permite registrar y realizar seguimiento de incidencias.

Información gestionada:

- Título del ticket
- Descripción del problema
- Fecha de creación
- Estado
- Prioridad
- Usuario solicitante

---

### Entidad

Representa organizaciones o clientes asociados al sistema.

---

### Contacto

Permite registrar personas relacionadas con una entidad.

---

### TicketAdjunto

Gestiona archivos relacionados a cada ticket.

---

## Relación general

Usuario
↓
Ticket
↓
TicketAdjunto

Entidad
↓
Contacto

---

## Tecnología utilizada

- SQL Server
- Entity Framework
- Modelo basado en clases C#