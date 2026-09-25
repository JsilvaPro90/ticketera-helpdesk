using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Ticketera.Models;

namespace Ticketera.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private readonly string[] extensionesPermitidas =
        {
            ".jpg", ".jpeg", ".png", ".gif",
            ".pdf", ".doc", ".docx",
            ".xls", ".xlsx",
            ".txt"
        };

        private const int MaximoBytes = 10 * 1024 * 1024;

        private void CargarSesionSiFalta()
        {
            if (Session["Rol"] != null)
            {
                return;
            }

            if (!User.Identity.IsAuthenticated)
            {
                return;
            }

            string nombreUsuario = User.Identity.Name;

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.NombreUsuario == nombreUsuario &&
                u.Activo
            );

            if (usuario != null)
            {
                Session["UsuarioId"] = usuario.Id;
                Session["NombreCompleto"] = usuario.NombreCompleto;
                Session["Rol"] = usuario.Rol;
            }
        }

        private string RolActual()
        {
            CargarSesionSiFalta();

            if (Session["Rol"] == null)
            {
                return "";
            }

            return Session["Rol"].ToString();
        }

        private bool EsRolTecnico(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return false;
            }

            string rolNormalizado = rol.Trim().ToLower();

            return rolNormalizado == "tecnico" ||
                   rolNormalizado == "técnico";
        }

        private bool EsRolResponsable(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return false;
            }

            string rolNormalizado = rol.Trim().ToLower();

            return rolNormalizado == "admin" ||
                   rolNormalizado == "tecnico" ||
                   rolNormalizado == "técnico";
        }

        private bool PuedeEditarTicket()
        {
            string rol = RolActual();

            return rol == "Admin" || EsRolTecnico(rol);
        }

        private bool TicketEstaCerrado(Ticket ticket)
        {
            return ticket != null &&
                   !string.IsNullOrWhiteSpace(ticket.Estado) &&
                   ticket.Estado.Equals("Cerrado", StringComparison.OrdinalIgnoreCase);
        }

        private int UsuarioActualId()
        {
            CargarSesionSiFalta();

            if (Session["UsuarioId"] == null)
            {
                return 0;
            }

            return Convert.ToInt32(Session["UsuarioId"]);
        }

        private Usuario ObtenerUsuarioActual()
        {
            int usuarioId = UsuarioActualId();

            if (usuarioId == 0)
            {
                return null;
            }

            return db.Usuarios
                .Include(u => u.Contacto)
                .FirstOrDefault(u => u.Id == usuarioId && u.Activo);
        }

        private int? ObtenerEntidadClienteActual()
        {
            Usuario usuario = ObtenerUsuarioActual();

            if (usuario == null || usuario.Contacto == null)
            {
                return null;
            }

            return usuario.Contacto.EntidadId;
        }

        private bool ClientePuedeVerTicket(Ticket ticket)
        {
            if (ticket == null)
            {
                return false;
            }

            if (RolActual() != "Cliente")
            {
                return true;
            }

            int? entidadId = ObtenerEntidadClienteActual();

            if (!entidadId.HasValue)
            {
                return false;
            }

            return ticket.EntidadId == entidadId.Value;
        }

        private List<Persona> ObtenerResponsablesAsignables(int? personaSeleccionadaId = null)
        {
            var responsables = db.Personas
                .Where(p =>
                    p.Rol != null &&
                    (
                        p.Rol.Trim().ToLower() == "admin" ||
                        p.Rol.Trim().ToLower() == "tecnico" ||
                        p.Rol.Trim().ToLower() == "técnico"
                    )
                )
                .OrderBy(p => p.Nombre)
                .ToList();

            if (personaSeleccionadaId.HasValue)
            {
                bool yaExiste = responsables.Any(p => p.Id == personaSeleccionadaId.Value);

                if (!yaExiste)
                {
                    Persona personaActual = db.Personas.Find(personaSeleccionadaId.Value);

                    if (personaActual != null)
                    {
                        responsables.Add(personaActual);

                        responsables = responsables
                            .OrderBy(p => p.Nombre)
                            .ToList();
                    }
                }
            }

            return responsables;
        }

        private void CargarCombos(Ticket ticket = null)
        {
            int entidadSeleccionada = ticket != null ? ticket.EntidadId : 0;

            ViewBag.Tipos = new SelectList(
                new[] { "Incidente", "Solicitud", "Problema" },
                ticket != null ? ticket.Tipo : null
            );

            ViewBag.Categorias = new SelectList(
                new[]
                {
                    "Hardware",
                    "Software y Aplicaciones",
                    "Redes y Conectividad",
                    "Base de datos",
                    "Cuentas y Accesos",
                    "Correo Electrónico",
                    "Seguridad de la Información",
                    "Otros"
                },
                ticket != null ? ticket.Categoria : null
            );

            ViewBag.Estados = new SelectList(
                new[] { "Abierto", "En proceso", "Pendiente", "Resuelto", "Cerrado" },
                ticket != null ? ticket.Estado : null
            );

            ViewBag.Urgencias = new SelectList(
                new[] { "Baja", "Media", "Alta", "Muy alta" },
                ticket != null ? ticket.Urgencia : null
            );

            ViewBag.Preferencias = new SelectList(
                new[] { "Correo", "Teléfono", "WhatsApp" },
                ticket != null ? ticket.PreferenciaContacto : null
            );

            ViewBag.EntidadId = new SelectList(
                db.Entidades
                    .Where(e => e.Activo)
                    .OrderBy(e => e.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                ticket != null ? ticket.EntidadId : 0
            );

            var contactos = db.Contactos.Where(c => c.Activo);

            if (entidadSeleccionada > 0)
            {
                contactos = contactos.Where(c => c.EntidadId == entidadSeleccionada);
            }
            else
            {
                contactos = contactos.Where(c => false);
            }

            ViewBag.ContactoId = new SelectList(
                contactos
                    .OrderBy(c => c.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                ticket != null ? ticket.ContactoId : 0
            );

            var responsables = ObtenerResponsablesAsignables(
                ticket != null ? ticket.AsignadoAId : null
            );

            ViewBag.AsignadoAId = new SelectList(
                responsables,
                "Id",
                "Nombre",
                ticket != null ? ticket.AsignadoAId : null
            );
        }

        private void CargarCombosEdicion(Ticket ticket)
        {
            ViewBag.Tipos = new SelectList(
                new[] { "Incidente", "Solicitud", "Problema" },
                ticket.Tipo
            );

            ViewBag.Categorias = new SelectList(
                new[]
                {
                    "Hardware",
                    "Software y Aplicaciones",
                    "Redes y Conectividad",
                    "Base de datos",
                    "Cuentas y Accesos",
                    "Correo Electrónico",
                    "Seguridad de la Información",
                    "Otros"
                },
                ticket.Categoria
            );

            ViewBag.Estados = new SelectList(
                new[] { "Abierto", "En proceso", "Pendiente", "Resuelto", "Cerrado" },
                ticket.Estado
            );

            ViewBag.Urgencias = new SelectList(
                new[] { "Baja", "Media", "Alta", "Muy alta" },
                ticket.Urgencia
            );

            ViewBag.Preferencias = new SelectList(
                new[] { "Correo", "Teléfono", "WhatsApp" },
                ticket.PreferenciaContacto
            );

            ViewBag.EntidadId = new SelectList(
                db.Entidades
                    .Where(e => e.Activo)
                    .OrderBy(e => e.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                ticket.EntidadId
            );

            ViewBag.ContactoId = new SelectList(
                db.Contactos
                    .Where(c => c.Activo && c.EntidadId == ticket.EntidadId)
                    .OrderBy(c => c.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                ticket.ContactoId
            );

            var responsables = ObtenerResponsablesAsignables(ticket.AsignadoAId);

            ViewBag.AsignadoAId = new SelectList(
                responsables,
                "Id",
                "Nombre",
                ticket.AsignadoAId
            );

            ViewBag.TecnicosAsignables = responsables
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Nombre,
                    Selected = ticket.AsignadoAId.HasValue && p.Id == ticket.AsignadoAId.Value
                })
                .ToList();
        }

        // GET: Tickets
        public ActionResult Index()
        {
            var ticketsQuery = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .AsQueryable();

            if (RolActual() == "Cliente")
            {
                int? entidadId = ObtenerEntidadClienteActual();

                if (!entidadId.HasValue)
                {
                    return View(new List<Ticket>());
                }

                ticketsQuery = ticketsQuery.Where(t => t.EntidadId == entidadId.Value);
            }

            var tickets = ticketsQuery
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();

            return View(tickets);
        }

        // GET: Tickets/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .Include(t => t.Adjuntos)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (!ClientePuedeVerTicket(ticket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public ActionResult Create()
        {
            Ticket ticket = new Ticket
            {
                FechaApertura = DateTime.Now,
                Estado = "Abierto",
                Tipo = "Incidente",
                Urgencia = "Media",
                PreferenciaContacto = "Correo"
            };

            if (RolActual() == "Cliente")
            {
                Usuario usuario = ObtenerUsuarioActual();

                if (usuario == null || usuario.Contacto == null)
                {
                    return new HttpStatusCodeResult(
                        HttpStatusCode.Forbidden,
                        "Tu usuario cliente no tiene un contacto asociado."
                    );
                }

                ticket.EntidadId = usuario.Contacto.EntidadId;
                ticket.ContactoId = usuario.Contacto.Id;
                ticket.AsignadoAId = null;

                Entidad entidad = db.Entidades.Find(usuario.Contacto.EntidadId);

                ViewBag.EsCliente = true;
                ViewBag.EmpresaCliente = entidad != null ? entidad.Nombre : "";
                ViewBag.ContactoCliente = usuario.Contacto.Nombre;
            }
            else
            {
                ViewBag.EsCliente = false;
            }

            CargarCombos(ticket);

            return View(ticket);
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Ticket ticket, IEnumerable<HttpPostedFileBase> archivos)
        {
            ModelState.Remove("Entidad");
            ModelState.Remove("Contacto");
            ModelState.Remove("AsignadoA");
            ModelState.Remove("Adjuntos");

            if (ticket.AsignadoAId.HasValue && ticket.AsignadoAId.Value == 0)
            {
                ticket.AsignadoAId = null;
            }

            if (RolActual() == "Cliente")
            {
                Usuario usuario = ObtenerUsuarioActual();

                if (usuario == null || usuario.Contacto == null)
                {
                    return new HttpStatusCodeResult(
                        HttpStatusCode.Forbidden,
                        "Tu usuario cliente no tiene un contacto asociado."
                    );
                }

                ticket.EntidadId = usuario.Contacto.EntidadId;
                ticket.ContactoId = usuario.Contacto.Id;
                ticket.AsignadoAId = null;

                ModelState.Remove("EntidadId");
                ModelState.Remove("ContactoId");
                ModelState.Remove("AsignadoAId");
                ModelState.Remove("Entidad");
                ModelState.Remove("Contacto");
                ModelState.Remove("AsignadoA");

                Entidad entidad = db.Entidades.Find(usuario.Contacto.EntidadId);

                ViewBag.EsCliente = true;
                ViewBag.EmpresaCliente = entidad != null ? entidad.Nombre : "";
                ViewBag.ContactoCliente = usuario.Contacto.Nombre;
            }
            else
            {
                ViewBag.EsCliente = false;
            }

            bool entidadExiste = db.Entidades.Any(e =>
                e.Id == ticket.EntidadId &&
                e.Activo
            );

            if (!entidadExiste)
            {
                ModelState.AddModelError("EntidadId", "La empresa seleccionada no existe o no está activa.");
            }

            bool contactoExiste = db.Contactos.Any(c =>
                c.Id == ticket.ContactoId &&
                c.EntidadId == ticket.EntidadId &&
                c.Activo
            );

            if (!contactoExiste)
            {
                ModelState.AddModelError("ContactoId", "El contacto seleccionado no pertenece a la empresa indicada o no está activo.");
            }

            if (ticket.AsignadoAId.HasValue)
            {
                int responsableId = ticket.AsignadoAId.Value;

                bool responsableExiste = db.Personas.Any(p =>
                    p.Id == responsableId &&
                    p.Rol != null &&
                    (
                        p.Rol.Trim().ToLower() == "admin" ||
                        p.Rol.Trim().ToLower() == "tecnico" ||
                        p.Rol.Trim().ToLower() == "técnico"
                    )
                );

                if (!responsableExiste)
                {
                    ModelState.AddModelError(
                        "AsignadoAId",
                        "El responsable seleccionado no existe o no tiene rol Técnico/Admin."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                CargarCombos(ticket);
                return View(ticket);
            }

            ticket.FechaCreacion = DateTime.Now;
            ticket.FechaActualizacion = DateTime.Now;

            if (ticket.FechaApertura == default(DateTime))
            {
                ticket.FechaApertura = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(ticket.Estado))
            {
                ticket.Estado = "Abierto";
            }

            if (ticket.Estado == "Resuelto" && !ticket.FechaResolucion.HasValue)
            {
                ticket.FechaResolucion = DateTime.Now;
            }

            if (ticket.Estado == "Cerrado" && !ticket.FechaCierre.HasValue)
            {
                ticket.FechaCierre = DateTime.Now;
            }

            try
            {
                db.Tickets.Add(ticket);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                string detalle = ex.Message;
                Exception interna = ex.InnerException;

                while (interna != null)
                {
                    detalle += " | " + interna.Message;
                    interna = interna.InnerException;
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el ticket. Verifica la empresa, el contacto y el responsable asignado. Detalle técnico: " + detalle
                );

                CargarCombos(ticket);
                return View(ticket);
            }

            GuardarAdjuntos(ticket.Id, archivos);

            return RedirectToAction("Details", new { id = ticket.Id });
        }

        // GET: Tickets/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!PuedeEditarTicket())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para editar tickets."
                );
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (TicketEstaCerrado(ticket))
            {
                TempData["Error"] = "Este ticket está cerrado y ya no puede ser editado.";
                return RedirectToAction("Details", new { id = ticket.Id });
            }

            CargarCombosEdicion(ticket);

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FormCollection form)
        {
            if (!PuedeEditarTicket())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para editar tickets."
                );
            }

            int id;

            if (!int.TryParse(form["Id"], out id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticketDb = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .FirstOrDefault(t => t.Id == id);

            if (ticketDb == null)
            {
                return HttpNotFound();
            }

            if (TicketEstaCerrado(ticketDb))
            {
                TempData["Error"] = "Este ticket está cerrado y ya no puede ser editado.";
                return RedirectToAction("Details", new { id = ticketDb.Id });
            }

            string tipo = form["Tipo"] != null ? form["Tipo"].Trim() : "";
            string categoria = form["Categoria"] != null ? form["Categoria"].Trim() : "";
            string estado = form["Estado"] != null ? form["Estado"].Trim() : "";
            string urgencia = form["Urgencia"] != null ? form["Urgencia"].Trim() : "";

            int? asignadoAId = null;

            if (!string.IsNullOrWhiteSpace(form["AsignadoAId"]))
            {
                int responsableId;

                if (int.TryParse(form["AsignadoAId"], out responsableId))
                {
                    if (responsableId > 0)
                    {
                        asignadoAId = responsableId;
                    }
                }
                else
                {
                    ModelState.AddModelError("AsignadoAId", "El responsable seleccionado no es válido.");
                }
            }

            if (string.IsNullOrWhiteSpace(tipo))
            {
                ModelState.AddModelError("Tipo", "Selecciona el tipo.");
            }

            if (string.IsNullOrWhiteSpace(categoria))
            {
                ModelState.AddModelError("Categoria", "Selecciona la categoría.");
            }

            if (string.IsNullOrWhiteSpace(estado))
            {
                ModelState.AddModelError("Estado", "Selecciona el estado.");
            }

            if (string.IsNullOrWhiteSpace(urgencia))
            {
                ModelState.AddModelError("Urgencia", "Selecciona la urgencia.");
            }

            if (asignadoAId.HasValue)
            {
                int responsableId = asignadoAId.Value;

                bool existeResponsable = db.Personas.Any(p =>
                    p.Id == responsableId &&
                    p.Rol != null &&
                    (
                        p.Rol.Trim().ToLower() == "admin" ||
                        p.Rol.Trim().ToLower() == "tecnico" ||
                        p.Rol.Trim().ToLower() == "técnico"
                    )
                );

                if (!existeResponsable)
                {
                    ModelState.AddModelError("AsignadoAId", "El responsable seleccionado no es válido.");
                }
            }

            if (!ModelState.IsValid)
            {
                ticketDb.Tipo = tipo;
                ticketDb.Categoria = categoria;
                ticketDb.Estado = estado;
                ticketDb.Urgencia = urgencia;
                ticketDb.AsignadoAId = asignadoAId;

                CargarCombosEdicion(ticketDb);

                return View(ticketDb);
            }

            string estadoAnterior = ticketDb.Estado;

            ticketDb.Tipo = tipo;
            ticketDb.Categoria = categoria;
            ticketDb.Estado = estado;
            ticketDb.Urgencia = urgencia;
            ticketDb.AsignadoAId = asignadoAId;
            ticketDb.FechaActualizacion = DateTime.Now;

            if (estado == "Resuelto" && estadoAnterior != "Resuelto")
            {
                if (!ticketDb.FechaResolucion.HasValue)
                {
                    ticketDb.FechaResolucion = DateTime.Now;
                }
            }

            if (estado == "Cerrado" && estadoAnterior != "Cerrado")
            {
                if (!ticketDb.FechaCierre.HasValue)
                {
                    ticketDb.FechaCierre = DateTime.Now;
                }

                if (!ticketDb.FechaResolucion.HasValue)
                {
                    ticketDb.FechaResolucion = DateTime.Now;
                }
            }

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                string detalle = ex.Message;
                Exception interna = ex.InnerException;

                while (interna != null)
                {
                    detalle += " | " + interna.Message;
                    interna = interna.InnerException;
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo actualizar el ticket. Detalle técnico: " + detalle
                );

                CargarCombosEdicion(ticketDb);
                return View(ticketDb);
            }

            TempData["Mensaje"] = "Ticket actualizado correctamente.";

            return RedirectToAction("Details", new { id = ticketDb.Id });
        }

        // GET: Tickets/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!PuedeEditarTicket())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para eliminar tickets."
                );
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .Include(t => t.Adjuntos)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!PuedeEditarTicket())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para eliminar tickets."
                );
            }

            Ticket ticket = db.Tickets
                .Include(t => t.Adjuntos)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            foreach (TicketAdjunto adjunto in ticket.Adjuntos.ToList())
            {
                EliminarArchivoFisico(adjunto.RutaArchivo);
            }

            db.Tickets.Remove(ticket);
            db.SaveChanges();

            TempData["Mensaje"] = "Ticket eliminado correctamente.";

            return RedirectToAction("Index");
        }

        // POST: Tickets/SubirAdjuntos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubirAdjuntos(int ticketId, IEnumerable<HttpPostedFileBase> archivos)
        {
            Ticket ticket = db.Tickets.Find(ticketId);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (!ClientePuedeVerTicket(ticket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (archivos == null || !archivos.Any(a => a != null && a.ContentLength > 0))
            {
                TempData["ErrorAdjunto"] = "Debes seleccionar al menos un archivo.";
                return RedirectToAction("Details", new { id = ticketId });
            }

            GuardarAdjuntos(ticketId, archivos);

            return RedirectToAction("Details", new { id = ticketId });
        }

        // GET: Tickets/VerAdjunto/5
        public ActionResult VerAdjunto(int id)
        {
            TicketAdjunto adjunto = db.TicketAdjuntos.Find(id);

            if (adjunto == null)
            {
                return HttpNotFound();
            }

            Ticket ticket = db.Tickets.Find(adjunto.TicketId);

            if (!ClientePuedeVerTicket(ticket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            string rutaFisica = Server.MapPath(adjunto.RutaArchivo);

            if (!System.IO.File.Exists(rutaFisica))
            {
                return HttpNotFound("Archivo no encontrado.");
            }

            return File(rutaFisica, adjunto.ContentType ?? "application/octet-stream");
        }

        // GET: Tickets/DescargarAdjunto/5
        public ActionResult DescargarAdjunto(int id)
        {
            TicketAdjunto adjunto = db.TicketAdjuntos.Find(id);

            if (adjunto == null)
            {
                return HttpNotFound();
            }

            Ticket ticket = db.Tickets.Find(adjunto.TicketId);

            if (!ClientePuedeVerTicket(ticket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            string rutaFisica = Server.MapPath(adjunto.RutaArchivo);

            if (!System.IO.File.Exists(rutaFisica))
            {
                return HttpNotFound("Archivo no encontrado.");
            }

            return File(
                rutaFisica,
                adjunto.ContentType ?? "application/octet-stream",
                adjunto.NombreOriginal
            );
        }

        // POST: Tickets/EliminarAdjunto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarAdjunto(int id)
        {
            TicketAdjunto adjunto = db.TicketAdjuntos.Find(id);

            if (adjunto == null)
            {
                return HttpNotFound();
            }

            Ticket ticket = db.Tickets.Find(adjunto.TicketId);

            if (!ClientePuedeVerTicket(ticket))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            int ticketId = adjunto.TicketId;

            EliminarArchivoFisico(adjunto.RutaArchivo);

            db.TicketAdjuntos.Remove(adjunto);
            db.SaveChanges();

            TempData["MensajeAdjunto"] = "Adjunto eliminado correctamente.";

            return RedirectToAction("Details", new { id = ticketId });
        }

        // AJAX: Tickets/ObtenerContactosPorEntidad?entidadId=1
        public JsonResult ObtenerContactosPorEntidad(int entidadId)
        {
            var contactos = db.Contactos
                .Where(c => c.EntidadId == entidadId && c.Activo)
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Email,
                    c.Telefono
                })
                .ToList();

            return Json(contactos, JsonRequestBehavior.AllowGet);
        }

        private void GuardarAdjuntos(int ticketId, IEnumerable<HttpPostedFileBase> archivos)
        {
            if (archivos == null)
            {
                return;
            }

            int guardados = 0;
            List<string> errores = new List<string>();

            foreach (HttpPostedFileBase archivo in archivos)
            {
                if (archivo == null || archivo.ContentLength == 0)
                {
                    continue;
                }

                string nombreOriginal = Path.GetFileName(archivo.FileName);
                string extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    errores.Add(nombreOriginal + " tiene una extensión no permitida.");
                    continue;
                }

                if (archivo.ContentLength > MaximoBytes)
                {
                    errores.Add(nombreOriginal + " supera el tamaño máximo de 10 MB.");
                    continue;
                }

                string nombreGuardado = Guid.NewGuid().ToString("N") + extension;

                string carpetaVirtual = "~/App_Data/AdjuntosTickets/" + ticketId;
                string carpetaFisica = Server.MapPath(carpetaVirtual);

                if (!Directory.Exists(carpetaFisica))
                {
                    Directory.CreateDirectory(carpetaFisica);
                }

                string rutaFisica = Path.Combine(carpetaFisica, nombreGuardado);
                string rutaVirtual = carpetaVirtual + "/" + nombreGuardado;

                archivo.SaveAs(rutaFisica);

                TicketAdjunto adjunto = new TicketAdjunto
                {
                    TicketId = ticketId,
                    NombreOriginal = nombreOriginal,
                    NombreGuardado = nombreGuardado,
                    RutaArchivo = rutaVirtual,
                    ContentType = archivo.ContentType,
                    Extension = extension,
                    TamanioBytes = archivo.ContentLength,
                    FechaSubida = DateTime.Now
                };

                db.TicketAdjuntos.Add(adjunto);
                guardados++;
            }

            db.SaveChanges();

            if (guardados > 0)
            {
                TempData["MensajeAdjunto"] = guardados + " archivo(s) adjuntado(s) correctamente.";
            }

            if (errores.Any())
            {
                TempData["ErrorAdjunto"] = string.Join(" ", errores);
            }
        }

        private void EliminarArchivoFisico(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
            {
                return;
            }

            string rutaFisica = Server.MapPath(rutaArchivo);

            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}