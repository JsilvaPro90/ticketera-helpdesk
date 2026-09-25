using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ticketera.Models;
using Ticketera.Models.ViewModels;
using Ticketera.Services;

namespace Ticketera.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

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

        private int UsuarioActualId()
        {
            CargarSesionSiFalta();

            if (Session["UsuarioId"] == null)
            {
                return 0;
            }

            return Convert.ToInt32(Session["UsuarioId"]);
        }

        private int? ObtenerEntidadClienteActual()
        {
            int usuarioId = UsuarioActualId();

            if (usuarioId == 0)
            {
                return null;
            }

            Usuario usuario = db.Usuarios
                .Include(u => u.Contacto)
                .FirstOrDefault(u => u.Id == usuarioId && u.Activo);

            if (usuario == null || usuario.Contacto == null)
            {
                return null;
            }

            return usuario.Contacto.EntidadId;
        }

        private DashboardViewModel CrearDashboardVacio()
        {
            return new DashboardViewModel
            {
                TotalTickets = 0,
                TicketsAbiertos = 0,
                TicketsEnProceso = 0,
                TicketsPendientes = 0,
                TicketsResueltos = 0,
                TicketsCerrados = 0,
                TicketsMuyAlta = 0,
                TicketsSinAsignar = 0,
                TicketsPorEstado = new List<DashboardItemViewModel>(),
                TicketsPorUrgencia = new List<DashboardItemViewModel>(),
                TicketsPorEmpresa = new List<DashboardItemViewModel>(),
                TicketsPorTecnico = new List<DashboardItemViewModel>(),
                UltimosTickets = new List<DashboardTicketViewModel>()
            };
        }

        public ActionResult ProbarCorreo()
        {
            string error;

            bool enviado = EmailService.EnviarCorreo(
                "j.silva.pro90@gmail.com",
                "Prueba de correo - Sistema de Tickets",
                "<h2>Correo de prueba</h2><p>El envío SMTP funciona correctamente desde el Sistema de Tickets.</p>",
                out error
            );

            if (enviado)
            {
                return Content("Correo enviado correctamente. Revisa tu bandeja de entrada o spam.");
            }

            return Content("Error al enviar correo: " + error);
        }

        public ActionResult VerConfigCorreo()
        {
            string host = ConfigurationManager.AppSettings["SmtpHost"];
            string port = ConfigurationManager.AppSettings["SmtpPort"];
            string user = ConfigurationManager.AppSettings["SmtpUser"];
            string from = ConfigurationManager.AppSettings["SmtpFrom"];
            string enableSsl = ConfigurationManager.AppSettings["SmtpEnableSsl"];
            string pass = ConfigurationManager.AppSettings["SmtpPass"];

            pass = pass == null ? "" : pass.Replace(" ", "").Trim();

            string resultado =
                "SmtpHost: " + host + "<br/>" +
                "SmtpPort: " + port + "<br/>" +
                "SmtpUser: " + user + "<br/>" +
                "SmtpFrom: " + from + "<br/>" +
                "SmtpEnableSsl: " + enableSsl + "<br/>" +
                "SmtpPass longitud: " + pass.Length + "<br/>";

            return Content(resultado, "text/html");
        }

        public ActionResult Index()
        {
            var ticketsQuery = db.Tickets
                .Include(t => t.Entidad)
                .Include(t => t.Contacto)
                .Include(t => t.AsignadoA)
                .AsNoTracking()
                .AsQueryable();

            if (RolActual() == "Cliente")
            {
                int? entidadId = ObtenerEntidadClienteActual();

                if (!entidadId.HasValue)
                {
                    return View(CrearDashboardVacio());
                }

                ticketsQuery = ticketsQuery.Where(t => t.EntidadId == entidadId.Value);
            }

            var tickets = ticketsQuery.ToList();

            int total = tickets.Count;

            Func<int, int> porcentaje = cantidad =>
            {
                if (total == 0)
                {
                    return 0;
                }

                return (int)Math.Round((cantidad * 100.0) / total);
            };

            var model = new DashboardViewModel
            {
                TotalTickets = total,

                TicketsAbiertos = tickets.Count(t => t.Estado == "Abierto"),
                TicketsEnProceso = tickets.Count(t => t.Estado == "En proceso"),
                TicketsPendientes = tickets.Count(t => t.Estado == "Pendiente"),
                TicketsResueltos = tickets.Count(t => t.Estado == "Resuelto"),
                TicketsCerrados = tickets.Count(t => t.Estado == "Cerrado"),
                TicketsMuyAlta = tickets.Count(t => t.Urgencia == "Muy alta"),
                TicketsSinAsignar = tickets.Count(t => t.AsignadoAId == null),

                TicketsPorEstado = new[]
                {
                    "Abierto",
                    "En proceso",
                    "Pendiente",
                    "Resuelto",
                    "Cerrado"
                }
                .Select(estado =>
                {
                    int cantidad = tickets.Count(t => t.Estado == estado);

                    return new DashboardItemViewModel
                    {
                        Nombre = estado,
                        Cantidad = cantidad,
                        Porcentaje = porcentaje(cantidad)
                    };
                })
                .ToList(),

                TicketsPorUrgencia = new[]
                {
                    "Muy alta",
                    "Alta",
                    "Media",
                    "Baja"
                }
                .Select(urgencia =>
                {
                    int cantidad = tickets.Count(t => t.Urgencia == urgencia);

                    return new DashboardItemViewModel
                    {
                        Nombre = urgencia,
                        Cantidad = cantidad,
                        Porcentaje = porcentaje(cantidad)
                    };
                })
                .ToList(),

                TicketsPorEmpresa = tickets
                    .GroupBy(t => t.Entidad != null ? t.Entidad.Nombre : "Sin empresa")
                    .Select(g => new DashboardItemViewModel
                    {
                        Nombre = g.Key,
                        Cantidad = g.Count(),
                        Porcentaje = porcentaje(g.Count())
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .Take(8)
                    .ToList(),

                TicketsPorTecnico = tickets
                    .GroupBy(t => t.AsignadoA != null ? t.AsignadoA.Nombre : "Sin asignar")
                    .Select(g => new DashboardItemViewModel
                    {
                        Nombre = g.Key,
                        Cantidad = g.Count(),
                        Porcentaje = porcentaje(g.Count())
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .Take(8)
                    .ToList(),

                UltimosTickets = tickets
                    .OrderByDescending(t => t.FechaCreacion)
                    .Take(8)
                    .Select(t => new DashboardTicketViewModel
                    {
                        Id = t.Id,
                        Titulo = t.Titulo,
                        Empresa = t.Entidad != null ? t.Entidad.Nombre : "Sin empresa",
                        Contacto = t.Contacto != null ? t.Contacto.Nombre : "Sin contacto",
                        Tecnico = t.AsignadoA != null ? t.AsignadoA.Nombre : "Sin asignar",
                        Estado = t.Estado,
                        Urgencia = t.Urgencia,
                        FechaApertura = t.FechaApertura
                    })
                    .ToList()
            };

            return View(model);
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