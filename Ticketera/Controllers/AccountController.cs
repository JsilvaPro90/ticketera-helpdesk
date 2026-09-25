using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using Ticketera.Models;
using Ticketera.Models.ViewModels;
using Ticketera.Services;

namespace Ticketera.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool existeUsuario = db.Usuarios.Any(u => u.NombreUsuario == model.NombreUsuario);

            if (existeUsuario)
            {
                ModelState.AddModelError("NombreUsuario", "Este usuario ya existe.");
                return View(model);
            }

            bool existeCorreo = db.Usuarios.Any(u => u.Email == model.Email);

            if (existeCorreo)
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                return View(model);
            }

            Usuario usuario = new Usuario
            {
                NombreUsuario = model.NombreUsuario.Trim(),
                NombreCompleto = model.NombreCompleto.Trim(),
                Email = model.Email.Trim(),
                PasswordHash = Crypto.HashPassword(model.Password),
                Rol = "Cliente",
                Activo = true,
                FechaRegistro = DateTime.Now,
                RequiereCambioPassword = false,
                FechaUltimoCambioPassword = DateTime.Now
            };

            db.Usuarios.Add(usuario);
            db.SaveChanges();

            TempData["MensajeLogin"] = "Cuenta creada correctamente. Ahora puedes iniciar sesión.";

            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string texto = model.UsuarioOEmail.Trim();

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.Activo &&
                (u.NombreUsuario == texto || u.Email == texto)
            );

            if (usuario != null)
            {
                string token = Guid.NewGuid().ToString("N");

                usuario.ResetToken = token;
                usuario.ResetTokenExpira = DateTime.Now.AddMinutes(30);

                db.SaveChanges();

                string link = GenerarLinkResetPassword(token);

                string asunto = "Cambio de contraseña - Sistema de Tickets";

                string cuerpo = CrearCuerpoCorreoCambioClave(usuario, link);

                string errorCorreo;

                bool enviado = EmailService.EnviarCorreo(
                    usuario.Email,
                    asunto,
                    cuerpo,
                    out errorCorreo
                );

                if (!enviado)
                {
                    TempData["Error"] = "No se pudo enviar el correo. Detalle: " + errorCorreo;
                    return View(model);
                }
            }

            TempData["Mensaje"] = "Si la cuenta existe, se envió un enlace de recuperación al correo registrado.";

            return View();
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login");
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.ResetToken == token &&
                u.ResetTokenExpira > DateTime.Now &&
                u.Activo
            );

            if (usuario == null)
            {
                return Content("El enlace de recuperación no es válido o ya expiró.");
            }

            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.ResetToken == model.Token &&
                u.ResetTokenExpira > DateTime.Now &&
                u.Activo
            );

            if (usuario == null)
            {
                return Content("El enlace de recuperación no es válido o ya expiró.");
            }

            usuario.PasswordHash = Crypto.HashPassword(model.NuevaPassword);
            usuario.ResetToken = null;
            usuario.ResetTokenExpira = null;
            usuario.RequiereCambioPassword = false;
            usuario.FechaUltimoCambioPassword = DateTime.Now;

            db.SaveChanges();

            TempData["MensajeLogin"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";

            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string usuarioTexto = model.NombreUsuario.Trim();

            Usuario usuario = db.Usuarios
                .FirstOrDefault(u =>
                    u.NombreUsuario == usuarioTexto &&
                    u.Activo
                );

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            bool passwordCorrecto = Crypto.VerifyHashedPassword(
                usuario.PasswordHash,
                model.Password
            );

            if (!passwordCorrecto)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            FormsAuthentication.SetAuthCookie(usuario.NombreUsuario, model.Recordarme);

            Session["UsuarioId"] = usuario.Id;
            Session["NombreCompleto"] = usuario.NombreCompleto;
            Session["Rol"] = usuario.Rol;

            if (usuario.RequiereCambioPassword)
            {
                return RedirectToAction("CambiarPasswordObligatorio", "Account");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult CambiarPasswordObligatorio()
        {
            int usuarioId = ObtenerUsuarioActualId();

            if (usuarioId == 0)
            {
                return RedirectToAction("Login");
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.Id == usuarioId &&
                u.Activo
            );

            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            if (!usuario.RequiereCambioPassword)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new CambiarPasswordObligatorioViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarPasswordObligatorio(CambiarPasswordObligatorioViewModel model)
        {
            int usuarioId = ObtenerUsuarioActualId();

            if (usuarioId == 0)
            {
                return RedirectToAction("Login");
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.Id == usuarioId &&
                u.Activo
            );

            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            usuario.PasswordHash = Crypto.HashPassword(model.NuevaPassword);
            usuario.RequiereCambioPassword = false;
            usuario.FechaUltimoCambioPassword = DateTime.Now;
            usuario.ResetToken = null;
            usuario.ResetTokenExpira = null;

            db.SaveChanges();

            TempData["Mensaje"] = "Contraseña actualizada correctamente.";

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }

        private int ObtenerUsuarioActualId()
        {
            if (Session["UsuarioId"] == null)
            {
                return 0;
            }

            return Convert.ToInt32(Session["UsuarioId"]);
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
                Este enlace estará disponible por 30 minutos.
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