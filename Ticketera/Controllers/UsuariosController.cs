using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using Ticketera.Models;
using Ticketera.Models.ViewModels;
using Ticketera.Services;

namespace Ticketera.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
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

        private bool EsAdmin()
        {
            CargarSesionSiFalta();

            return Session["Rol"] != null &&
                   Session["Rol"].ToString() == "Admin";
        }

        private bool EsTecnico()
        {
            CargarSesionSiFalta();

            return Session["Rol"] != null &&
                   (
                       Session["Rol"].ToString() == "Tecnico" ||
                       Session["Rol"].ToString() == "Técnico"
                   );
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

        private ActionResult ValidarAdminOTecnico()
        {
            if (!EsAdmin() && !EsTecnico())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para acceder a esta sección."
                );
            }

            return null;
        }

        private bool TecnicoPuedeGestionarUsuario(Usuario usuario)
        {
            if (!EsTecnico())
            {
                return true;
            }

            return usuario.Rol == "Cliente";
        }

        // GET: Usuarios
        public ActionResult Index()
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            var usuarios = db.Usuarios.AsQueryable();

            if (EsTecnico())
            {
                usuarios = usuarios.Where(u => u.Rol == "Cliente");
            }

            var lista = usuarios
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.NombreCompleto)
                .ToList();

            return View(lista);
        }

        // GET: Usuarios/Create
        public ActionResult Create()
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            UsuarioCreateViewModel model = new UsuarioCreateViewModel
            {
                Activo = true,
                Rol = EsTecnico() ? "Cliente" : null
            };

            CargarRoles(model.Rol);
            CargarEmpresas(model.EntidadId);

            return View(model);
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UsuarioCreateViewModel model)
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            if (EsTecnico())
            {
                model.Rol = "Cliente";
            }

            if (model.Rol == "Cliente" && !model.EntidadId.HasValue)
            {
                ModelState.AddModelError("EntidadId", "Debes seleccionar una empresa para el cliente.");
            }

            if (!ModelState.IsValid)
            {
                CargarRoles(model.Rol);
                CargarEmpresas(model.EntidadId);
                return View(model);
            }

            string nombreUsuario = model.NombreUsuario.Trim();
            string email = model.Email.Trim();

            bool existeUsuario = db.Usuarios.Any(u => u.NombreUsuario == nombreUsuario);

            if (existeUsuario)
            {
                ModelState.AddModelError("NombreUsuario", "Este usuario ya existe.");
                CargarRoles(model.Rol);
                CargarEmpresas(model.EntidadId);
                return View(model);
            }

            bool existeEmail = db.Usuarios.Any(u => u.Email == email);

            if (existeEmail)
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                CargarRoles(model.Rol);
                CargarEmpresas(model.EntidadId);
                return View(model);
            }

            int? contactoId = null;

            if (model.Rol == "Cliente")
            {
                Contacto contacto = new Contacto
                {
                    EntidadId = model.EntidadId.Value,
                    Nombre = model.NombreCompleto.Trim(),
                    Email = email,
                    Telefono = model.Telefono,
                    Cargo = model.Cargo,
                    Activo = true,
                    EsPrincipal = false,
                    FechaRegistro = DateTime.Now
                };

                db.Contactos.Add(contacto);
                db.SaveChanges();

                contactoId = contacto.Id;
            }

            Usuario usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                NombreCompleto = model.NombreCompleto.Trim(),
                Email = email,
                PasswordHash = Crypto.HashPassword(model.Password),
                Rol = model.Rol,
                Activo = model.Activo,
                FechaRegistro = DateTime.Now,
                ContactoId = contactoId,
                CreadoPorUsuarioId = UsuarioActualId(),
                RequiereCambioPassword = false,
                FechaUltimoCambioPassword = DateTime.Now
            };

            db.Usuarios.Add(usuario);
            db.SaveChanges();

            if (usuario.Rol == "Tecnico" || usuario.Rol == "Técnico")
            {
                CrearOActualizarPersonaTecnico(usuario);
            }

            TempData["Mensaje"] = "Usuario creado correctamente.";

            return RedirectToAction("Index");
        }

        // GET: Usuarios/Edit/5
        public ActionResult Edit(int? id)
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Usuario usuario = db.Usuarios
                .Include(u => u.Contacto)
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            if (!TecnicoPuedeGestionarUsuario(usuario))
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "Los técnicos solo pueden editar usuarios clientes."
                );
            }

            CargarRoles(usuario.Rol);

            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usuario model)
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            ModelState.Remove("PasswordHash");
            ModelState.Remove("ResetToken");
            ModelState.Remove("ResetTokenExpira");
            ModelState.Remove("Contacto");

            if (EsTecnico())
            {
                model.Rol = "Cliente";
            }

            if (!ModelState.IsValid)
            {
                CargarRoles(model.Rol);
                return View(model);
            }

            Usuario usuarioDb = db.Usuarios.Find(model.Id);

            if (usuarioDb == null)
            {
                return HttpNotFound();
            }

            if (!TecnicoPuedeGestionarUsuario(usuarioDb))
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "Los técnicos solo pueden editar usuarios clientes."
                );
            }

            string nombreUsuario = model.NombreUsuario.Trim();
            string email = model.Email != null ? model.Email.Trim() : null;

            bool usuarioDuplicado = db.Usuarios.Any(u =>
                u.Id != usuarioDb.Id &&
                u.NombreUsuario == nombreUsuario
            );

            if (usuarioDuplicado)
            {
                ModelState.AddModelError("NombreUsuario", "Este nombre de usuario ya existe.");
                CargarRoles(model.Rol);
                return View(model);
            }

            bool emailDuplicado = db.Usuarios.Any(u =>
                u.Id != usuarioDb.Id &&
                u.Email == email
            );

            if (!string.IsNullOrWhiteSpace(email) && emailDuplicado)
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                CargarRoles(model.Rol);
                return View(model);
            }

            usuarioDb.NombreUsuario = nombreUsuario;
            usuarioDb.NombreCompleto = model.NombreCompleto.Trim();
            usuarioDb.Email = email;
            usuarioDb.Rol = model.Rol;
            usuarioDb.Activo = model.Activo;

            db.SaveChanges();

            if (usuarioDb.Rol == "Tecnico" || usuarioDb.Rol == "Técnico")
            {
                CrearOActualizarPersonaTecnico(usuarioDb);
            }

            if (usuarioDb.Rol == "Cliente")
            {
                ActualizarContactoCliente(usuarioDb);
            }

            TempData["Mensaje"] = "Usuario actualizado correctamente.";

            return RedirectToAction("Index");
        }

        // POST: Usuarios/EnviarResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarResetPassword(int id)
        {
            var validar = ValidarAdminOTecnico();

            if (validar != null)
            {
                return validar;
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.Id == id &&
                u.Activo
            );

            if (usuario == null)
            {
                TempData["Error"] = "No se encontró el usuario o se encuentra inactivo.";
                return RedirectToAction("Index");
            }

            /*
             IMPORTANTE:
             No se valida el rol del usuario destino.
             El enlace de cambio de clave aplica para Admin, Tecnico y Cliente.
            */

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                TempData["Error"] = "El usuario " + usuario.NombreUsuario + " no tiene correo registrado.";
                return RedirectToAction("Index");
            }

            string correoDestino = usuario.Email.Trim();

            string token = Guid.NewGuid().ToString("N");

            usuario.ResetToken = token;
            usuario.ResetTokenExpira = DateTime.Now.AddHours(2);

            db.SaveChanges();

            string link = GenerarLinkResetPassword(token);

            string asunto = "Cambio de contraseña - Sistema de Tickets";

            string cuerpo = CrearCuerpoCorreoCambioClave(usuario, link);

            string errorCorreo;

            bool enviado = EmailService.EnviarCorreo(
                correoDestino,
                asunto,
                cuerpo,
                out errorCorreo
            );

            if (enviado)
            {
                TempData["Mensaje"] =
                    "El correo fue aceptado por el servidor SMTP. Usuario: " +
                    usuario.NombreUsuario +
                    " | Rol: " +
                    usuario.Rol +
                    " | Destino: " +
                    correoDestino +
                    ". Revisa Bandeja de entrada, Spam, Todos y Enviados del correo emisor.";
            }
            else
            {
                TempData["Error"] =
                    "No se pudo enviar el correo al usuario " +
                    usuario.NombreUsuario +
                    " | Rol: " +
                    usuario.Rol +
                    " | Destino: " +
                    correoDestino +
                    ". Detalle técnico: " +
                    errorCorreo;
            }

            return RedirectToAction("Index");
        }

        private string GenerarLinkResetPassword(string token)
        {
            string baseUrl = ConfigurationManager.AppSettings["AppBaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
            }

            string ruta = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token }
            );

            return baseUrl.TrimEnd('/') + ruta;
        }

        private string CrearCuerpoCorreoCambioClave(Usuario usuario, string link)
        {
            string nombre = WebUtility.HtmlEncode(usuario.NombreCompleto);
            string nombreUsuario = WebUtility.HtmlEncode(usuario.NombreUsuario);
            string rol = WebUtility.HtmlEncode(usuario.Rol);
            string linkSeguro = WebUtility.HtmlEncode(link);

            string cuerpo = @"
<html>
<body style='margin:0; padding:0; background:#f1f5f9; font-family:Segoe UI, Arial, sans-serif;'>

    <div style='max-width:650px; margin:30px auto; background:#ffffff; border-radius:18px; overflow:hidden; box-shadow:0 15px 35px rgba(15,23,42,0.12);'>

        <div style='background:linear-gradient(135deg,#0db6c9,#20c96b); padding:28px 32px; color:#ffffff;'>
            <h2 style='margin:0; font-size:26px;'>Sistema de Tickets</h2>
            <p style='margin:8px 0 0 0; font-size:14px;'>Cambio de contraseña</p>
        </div>

        <div style='padding:32px;'>

            <h3 style='margin:0 0 15px 0; color:#0f172a; font-size:23px;'>
                Hola " + nombre + @"
            </h3>

            <p style='color:#334155; font-size:15px; line-height:1.6;'>
                Se ha generado una solicitud para cambiar la contraseña de tu usuario en el Sistema de Tickets.
            </p>

            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:14px; padding:20px; margin:24px 0;'>
                <p style='margin:0 0 8px 0;'><b>Usuario:</b> " + nombreUsuario + @"</p>
                <p style='margin:0;'><b>Rol:</b> " + rol + @"</p>
            </div>

            <p style='color:#334155; font-size:15px; line-height:1.6;'>
                Para cambiar tu contraseña, haz clic en el siguiente botón:
            </p>

            <p style='margin-top:24px;'>
                <a href='" + linkSeguro + @"'
                   style='display:inline-block; background:#0db6c9; color:#ffffff; text-decoration:none; padding:12px 22px; border-radius:12px; font-weight:700;'>
                    Cambiar contraseña
                </a>
            </p>

            <p style='color:#64748b; font-size:13px; line-height:1.5; margin-top:26px;'>
                Este enlace estará disponible por 2 horas.
            </p>

            <p style='color:#64748b; font-size:13px; line-height:1.5;'>
                Si no solicitaste este cambio, comunícate con el administrador del sistema.
            </p>

        </div>
    </div>

</body>
</html>";

            return cuerpo;
        }

        private void CargarRoles(string rolSeleccionado = null)
        {
            if (EsAdmin())
            {
                ViewBag.Roles = new SelectList(
                    new[] { "Admin", "Tecnico", "Cliente" },
                    rolSeleccionado
                );
            }
            else if (EsTecnico())
            {
                ViewBag.Roles = new SelectList(
                    new[] { "Cliente" },
                    "Cliente"
                );
            }
        }

        private void CargarEmpresas(int? entidadId = null)
        {
            ViewBag.EntidadId = new SelectList(
                db.Entidades
                    .Where(e => e.Activo)
                    .OrderBy(e => e.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                entidadId
            );
        }

        private void CrearOActualizarPersonaTecnico(Usuario usuario)
        {
            if (usuario == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                return;
            }

            string email = usuario.Email.Trim();

            Persona persona = db.Personas.FirstOrDefault(p =>
                p.Email == email ||
                p.Nombre == usuario.NombreCompleto
            );

            if (persona == null)
            {
                persona = new Persona
                {
                    Nombre = usuario.NombreCompleto,
                    Email = email,
                    Rol = "Tecnico"
                };

                db.Personas.Add(persona);
            }
            else
            {
                persona.Nombre = usuario.NombreCompleto;
                persona.Email = email;
                persona.Rol = "Tecnico";
            }

            db.SaveChanges();
        }

        private void ActualizarContactoCliente(Usuario usuario)
        {
            if (!usuario.ContactoId.HasValue)
            {
                return;
            }

            Contacto contacto = db.Contactos.Find(usuario.ContactoId.Value);

            if (contacto == null)
            {
                return;
            }

            contacto.Nombre = usuario.NombreCompleto;
            contacto.Email = usuario.Email;
            contacto.Activo = usuario.Activo;

            db.SaveChanges();
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