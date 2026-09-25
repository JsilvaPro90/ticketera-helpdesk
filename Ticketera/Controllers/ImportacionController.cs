using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Ticketera.Models;
using Ticketera.Services;

namespace Ticketera.Controllers
{
    [Authorize]
    public class ImportacionController : Controller
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

        private bool EsAdmin()
        {
            return RolActual() == "Admin";
        }

        private int? UsuarioActualId()
        {
            CargarSesionSiFalta();

            if (Session["UsuarioId"] == null)
            {
                return null;
            }

            return Convert.ToInt32(Session["UsuarioId"]);
        }

        public ActionResult Index()
        {
            if (!EsAdmin())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para acceder a la carga masiva."
                );
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CargarExcel(HttpPostedFileBase archivo)
        {
            if (!EsAdmin())
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para realizar la carga masiva."
                );
            }

            if (archivo == null || archivo.ContentLength == 0)
            {
                TempData["Error"] = "Debes seleccionar un archivo Excel.";
                return RedirectToAction("Index");
            }

            string extension = Path.GetExtension(archivo.FileName).ToLower();

            if (extension != ".xlsx")
            {
                TempData["Error"] = "El archivo debe tener formato .xlsx.";
                return RedirectToAction("Index");
            }

            int empresasCreadas = 0;
            int empresasActualizadas = 0;
            int contactosCreados = 0;
            int contactosActualizados = 0;
            int usuariosCreados = 0;
            int usuariosActualizados = 0;
            int tecnicosCreados = 0;
            int tecnicosActualizados = 0;
            int personasCreadas = 0;
            int personasActualizadas = 0;
            int correosEnviados = 0;
            int correosFallidos = 0;

            List<string> errores = new List<string>();

            try
            {
                using (XLWorkbook workbook = new XLWorkbook(archivo.InputStream))
                {
                    IXLWorksheet hojaEmpresas = BuscarHoja(workbook, "EMPRESAS", "EMPRESA");
                    IXLWorksheet hojaContactos = BuscarHoja(workbook, "CONTACTO", "CONTACTOS");
                    IXLWorksheet hojaTecnicos = BuscarHoja(workbook, "TECNICOS", "TECNICO", "TÉCNICOS", "TÉCNICO");

                    bool encontroAlgunaHoja = hojaEmpresas != null || hojaContactos != null || hojaTecnicos != null;

                    if (!encontroAlgunaHoja)
                    {
                        TempData["Error"] = "No se encontró ninguna hoja válida. Usa EMPRESAS, CONTACTO o TECNICOS.";
                        return RedirectToAction("Index");
                    }

                    if (hojaEmpresas != null)
                    {
                        ProcesarEmpresas(
                            hojaEmpresas,
                            ref empresasCreadas,
                            ref empresasActualizadas,
                            errores
                        );

                        db.SaveChanges();
                    }

                    if (hojaContactos != null)
                    {
                        ProcesarContactosYUsuarios(
                            hojaContactos,
                            ref contactosCreados,
                            ref contactosActualizados,
                            ref usuariosCreados,
                            ref usuariosActualizados,
                            ref correosEnviados,
                            ref correosFallidos,
                            errores
                        );

                        db.SaveChanges();
                    }

                    if (hojaTecnicos != null)
                    {
                        ProcesarTecnicos(
                            hojaTecnicos,
                            ref tecnicosCreados,
                            ref tecnicosActualizados,
                            ref personasCreadas,
                            ref personasActualizadas,
                            errores
                        );

                        db.SaveChanges();
                    }
                }

                TempData["Mensaje"] =
                    "Carga finalizada correctamente. " +
                    "Empresas creadas: " + empresasCreadas + ". " +
                    "Empresas actualizadas: " + empresasActualizadas + ". " +
                    "Contactos creados: " + contactosCreados + ". " +
                    "Contactos actualizados: " + contactosActualizados + ". " +
                    "Usuarios clientes creados: " + usuariosCreados + ". " +
                    "Usuarios clientes actualizados: " + usuariosActualizados + ". " +
                    "Técnicos usuarios creados: " + tecnicosCreados + ". " +
                    "Técnicos usuarios actualizados: " + tecnicosActualizados + ". " +
                    "Responsables creados en Personas: " + personasCreadas + ". " +
                    "Responsables actualizados en Personas: " + personasActualizadas + ". " +
                    "Correos enviados: " + correosEnviados + ". " +
                    "Correos fallidos: " + correosFallidos + ".";

                if (errores.Any())
                {
                    TempData["Error"] = string.Join("<br/>", errores);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar el Excel: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private void ProcesarEmpresas(
            IXLWorksheet hoja,
            ref int creadas,
            ref int actualizadas,
            List<string> errores)
        {
            Dictionary<string, int> columnas = ObtenerColumnas(hoja);

            int ultimaFila = hoja.LastRowUsed() != null ? hoja.LastRowUsed().RowNumber() : 1;

            for (int fila = 2; fila <= ultimaFila; fila++)
            {
                string nombre = LeerCelda(hoja, columnas, fila, "Nombre de la empresa", "Nombre", "Empresa");
                string ruc = LeerCelda(hoja, columnas, fila, "RUC");
                string direccion = LeerCelda(hoja, columnas, fila, "Dirección", "Direccion");
                string correo = LeerCelda(hoja, columnas, fila, "Correo", "Email");
                string telefono = LeerCelda(hoja, columnas, fila, "Teléfono", "Telefono");
                bool activo = LeerBooleano(hoja, columnas, fila, true, "Empresa activa", "Activo");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    continue;
                }

                nombre = nombre.Trim();
                ruc = ruc.Trim();

                Entidad empresa = null;

                if (!string.IsNullOrWhiteSpace(ruc))
                {
                    empresa = db.Entidades
                        .AsEnumerable()
                        .FirstOrDefault(e => ObtenerValorTexto(e, "RUC", "Ruc", "Documento", "NumeroDocumento") == ruc);
                }

                if (empresa == null)
                {
                    empresa = db.Entidades.FirstOrDefault(e => e.Nombre == nombre);
                }

                if (empresa == null)
                {
                    empresa = new Entidad
                    {
                        Nombre = nombre,
                        Activo = activo
                    };

                    db.Entidades.Add(empresa);
                    creadas++;
                }
                else
                {
                    empresa.Nombre = nombre;
                    empresa.Activo = activo;
                    actualizadas++;
                }

                AsignarTextoSiExiste(empresa, ruc, "RUC", "Ruc", "Documento", "NumeroDocumento");
                AsignarTextoSiExiste(empresa, direccion, "Direccion", "Dirección", "Domicilio");
                AsignarTextoSiExiste(empresa, correo, "Correo", "Email", "CorreoElectronico", "CorreoElectrónico");
                AsignarTextoSiExiste(empresa, telefono, "Telefono", "Teléfono", "Celular");
            }
        }

        private void ProcesarContactosYUsuarios(
            IXLWorksheet hoja,
            ref int contactosCreados,
            ref int contactosActualizados,
            ref int usuariosCreados,
            ref int usuariosActualizados,
            ref int correosEnviados,
            ref int correosFallidos,
            List<string> errores)
        {
            Dictionary<string, int> columnas = ObtenerColumnas(hoja);

            int ultimaFila = hoja.LastRowUsed() != null ? hoja.LastRowUsed().RowNumber() : 1;

            for (int fila = 2; fila <= ultimaFila; fila++)
            {
                string nombreEmpresa = LeerCelda(hoja, columnas, fila, "Empresa");
                string nombreContacto = LeerCelda(hoja, columnas, fila, "Nombre completo", "Nombre");
                string cargo = LeerCelda(hoja, columnas, fila, "Cargo");
                string correo = LeerCelda(hoja, columnas, fila, "Correo", "Email");
                string telefono = LeerCelda(hoja, columnas, fila, "Teléfono", "Telefono");
                bool principal = LeerBooleano(hoja, columnas, fila, false, "Contacto principal", "Principal");
                bool activo = LeerBooleano(hoja, columnas, fila, true, "Contacto activo", "Activo");

                bool crearUsuario = LeerBooleano(hoja, columnas, fila, true, "Crear usuario", "CrearUsuario");
                bool enviarCredenciales = LeerBooleano(hoja, columnas, fila, true, "Enviar credenciales", "EnviarCredenciales");

                string usuarioExcel = LeerCelda(hoja, columnas, fila, "Usuario", "Nombre usuario", "NombreUsuario");

                if (string.IsNullOrWhiteSpace(nombreContacto))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nombreEmpresa))
                {
                    errores.Add("Fila " + fila + " de CONTACTO: falta la empresa.");
                    continue;
                }

                Entidad empresa = db.Entidades.FirstOrDefault(e => e.Nombre == nombreEmpresa.Trim());

                if (empresa == null)
                {
                    empresa = new Entidad
                    {
                        Nombre = nombreEmpresa.Trim(),
                        Activo = true
                    };

                    db.Entidades.Add(empresa);
                    db.SaveChanges();
                }

                Contacto contacto = ObtenerOCrearContacto(
                    empresa,
                    nombreContacto,
                    cargo,
                    correo,
                    telefono,
                    principal,
                    activo,
                    ref contactosCreados,
                    ref contactosActualizados
                );

                db.SaveChanges();

                if (crearUsuario)
                {
                    CrearOActualizarUsuarioCliente(
                        contacto,
                        usuarioExcel,
                        correo,
                        nombreContacto,
                        enviarCredenciales,
                        fila,
                        ref usuariosCreados,
                        ref usuariosActualizados,
                        ref correosEnviados,
                        ref correosFallidos,
                        errores
                    );
                }
            }
        }

        private void ProcesarTecnicos(
            IXLWorksheet hoja,
            ref int tecnicosCreados,
            ref int tecnicosActualizados,
            ref int personasCreadas,
            ref int personasActualizadas,
            List<string> errores)
        {
            Dictionary<string, int> columnas = ObtenerColumnas(hoja);

            int ultimaFila = hoja.LastRowUsed() != null ? hoja.LastRowUsed().RowNumber() : 1;

            for (int fila = 2; fila <= ultimaFila; fila++)
            {
                string rol = LeerCelda(hoja, columnas, fila, "Rol");
                string nombreUsuario = LeerCelda(hoja, columnas, fila, "Usuario", "Nombre usuario", "NombreUsuario");
                string nombreCompleto = LeerCelda(hoja, columnas, fila, "Nombre completo", "NombreCompleto", "Nombre");
                string correo = LeerCelda(hoja, columnas, fila, "Correo", "Email");
                string password = LeerCelda(hoja, columnas, fila, "Contraseña", "Contrasena", "Password");
                string confirmarPassword = LeerCelda(hoja, columnas, fila, "Confirmar contraseña", "Confirmar contrasena", "ConfirmarPassword");
                bool activo = LeerBooleano(hoja, columnas, fila, true, "Usuario activo", "Activo");

                if (string.IsNullOrWhiteSpace(nombreUsuario) &&
                    string.IsNullOrWhiteSpace(nombreCompleto) &&
                    string.IsNullOrWhiteSpace(correo))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nombreUsuario))
                {
                    errores.Add("Fila " + fila + " de TECNICOS: falta el usuario.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nombreCompleto))
                {
                    errores.Add("Fila " + fila + " de TECNICOS: falta el nombre completo.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(correo))
                {
                    errores.Add("Fila " + fila + " de TECNICOS: falta el correo.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    errores.Add("Fila " + fila + " de TECNICOS: falta la contraseña.");
                    continue;
                }

                if (password != confirmarPassword)
                {
                    errores.Add("Fila " + fila + " de TECNICOS: la contraseña y la confirmación no coinciden.");
                    continue;
                }

                if (password.Length < 6)
                {
                    errores.Add("Fila " + fila + " de TECNICOS: la contraseña debe tener mínimo 6 caracteres.");
                    continue;
                }

                rol = "Tecnico";

                nombreUsuario = nombreUsuario.Trim();
                nombreCompleto = nombreCompleto.Trim();
                correo = correo.Trim();

                Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario == nombreUsuario ||
                    u.Email == correo
                );

                if (usuario == null)
                {
                    usuario = new Usuario
                    {
                        NombreUsuario = nombreUsuario,
                        NombreCompleto = nombreCompleto,
                        Email = correo,
                        PasswordHash = Crypto.HashPassword(password),
                        Rol = rol,
                        Activo = activo,
                        FechaRegistro = DateTime.Now,
                        ContactoId = null,
                        CreadoPorUsuarioId = UsuarioActualId(),
                        RequiereCambioPassword = false,
                        FechaUltimoCambioPassword = DateTime.Now
                    };

                    db.Usuarios.Add(usuario);
                    tecnicosCreados++;
                }
                else
                {
                    usuario.NombreUsuario = nombreUsuario;
                    usuario.NombreCompleto = nombreCompleto;
                    usuario.Email = correo;
                    usuario.Rol = rol;
                    usuario.Activo = activo;
                    usuario.ContactoId = null;
                    usuario.PasswordHash = Crypto.HashPassword(password);
                    usuario.RequiereCambioPassword = false;
                    usuario.FechaUltimoCambioPassword = DateTime.Now;

                    tecnicosActualizados++;
                }

                Persona persona = db.Personas.FirstOrDefault(p =>
                    p.Email == correo ||
                    p.Nombre == nombreCompleto
                );

                if (persona == null)
                {
                    persona = new Persona
                    {
                        Nombre = nombreCompleto,
                        Email = correo,
                        Rol = "Tecnico"
                    };

                    db.Personas.Add(persona);
                    personasCreadas++;
                }
                else
                {
                    persona.Nombre = nombreCompleto;
                    persona.Email = correo;
                    persona.Rol = "Tecnico";

                    personasActualizadas++;
                }
            }
        }

        private Contacto ObtenerOCrearContacto(
            Entidad empresa,
            string nombreContacto,
            string cargo,
            string correo,
            string telefono,
            bool principal,
            bool activo,
            ref int contactosCreados,
            ref int contactosActualizados)
        {
            Contacto contacto = null;

            if (!string.IsNullOrWhiteSpace(correo))
            {
                string correoNormalizado = correo.Trim();

                contacto = db.Contactos.FirstOrDefault(c => c.Email == correoNormalizado);
            }

            if (contacto == null)
            {
                string nombreNormalizado = nombreContacto.Trim();

                contacto = db.Contactos.FirstOrDefault(c =>
                    c.EntidadId == empresa.Id &&
                    c.Nombre == nombreNormalizado
                );
            }

            if (contacto == null)
            {
                contacto = new Contacto
                {
                    EntidadId = empresa.Id,
                    Nombre = nombreContacto.Trim(),
                    Email = correo.Trim(),
                    Telefono = telefono.Trim(),
                    Activo = activo
                };

                db.Contactos.Add(contacto);
                contactosCreados++;
            }
            else
            {
                contacto.EntidadId = empresa.Id;
                contacto.Nombre = nombreContacto.Trim();
                contacto.Email = correo.Trim();
                contacto.Telefono = telefono.Trim();
                contacto.Activo = activo;

                contactosActualizados++;
            }

            AsignarTextoSiExiste(contacto, cargo, "Cargo", "Puesto", "Area", "Área");
            AsignarBooleanoSiExiste(contacto, principal, "Principal", "ContactoPrincipal", "EsPrincipal");

            return contacto;
        }

        private void CrearOActualizarUsuarioCliente(
            Contacto contacto,
            string usuarioExcel,
            string correo,
            string nombreContacto,
            bool enviarCredenciales,
            int fila,
            ref int usuariosCreados,
            ref int usuariosActualizados,
            ref int correosEnviados,
            ref int correosFallidos,
            List<string> errores)
        {
            if (contacto == null)
            {
                errores.Add("Fila " + fila + ": no se pudo crear el usuario porque no existe contacto.");
                return;
            }

            if (string.IsNullOrWhiteSpace(correo))
            {
                errores.Add("Fila " + fila + ": no se creó usuario para " + nombreContacto + " porque no tiene correo.");
                return;
            }

            string email = correo.Trim();

            string nombreUsuario = !string.IsNullOrWhiteSpace(usuarioExcel)
                ? usuarioExcel.Trim()
                : GenerarNombreUsuario(email, nombreContacto);

            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                errores.Add("Fila " + fila + ": no se pudo generar nombre de usuario.");
                return;
            }

            Usuario usuario = db.Usuarios.FirstOrDefault(u =>
                u.NombreUsuario == nombreUsuario ||
                u.Email == email
            );

            bool esNuevo = false;
            string passwordTemporal = "";

            if (usuario == null)
            {
                passwordTemporal = GenerarPasswordTemporal();

                usuario = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    NombreCompleto = nombreContacto.Trim(),
                    Email = email,
                    PasswordHash = Crypto.HashPassword(passwordTemporal),
                    Rol = "Cliente",
                    Activo = true,
                    FechaRegistro = DateTime.Now,
                    ContactoId = contacto.Id,
                    CreadoPorUsuarioId = UsuarioActualId(),
                    RequiereCambioPassword = true,
                    FechaUltimoCambioPassword = null
                };

                db.Usuarios.Add(usuario);
                usuariosCreados++;
                esNuevo = true;
            }
            else
            {
                usuario.NombreCompleto = nombreContacto.Trim();
                usuario.Email = email;
                usuario.Rol = "Cliente";
                usuario.Activo = true;
                usuario.ContactoId = contacto.Id;

                usuariosActualizados++;
            }

            db.SaveChanges();

            if (esNuevo && enviarCredenciales)
            {
                string errorCorreo;

                bool enviado = EnviarCorreoCredenciales(
                    usuario,
                    passwordTemporal,
                    out errorCorreo
                );

                if (enviado)
                {
                    correosEnviados++;
                }
                else
                {
                    correosFallidos++;
                    errores.Add("Fila " + fila + ": usuario creado, pero no se pudo enviar correo a " + email + ". Error: " + errorCorreo);
                }
            }
        }

        private bool EnviarCorreoCredenciales(Usuario usuario, string passwordTemporal, out string error)
        {
            error = "";

            string loginUrl = Url.Action(
                "Login",
                "Account",
                null,
                Request.Url.Scheme
            );

            string asunto = "Credenciales de acceso - Sistema de Tickets";

            string cuerpo = @"
<html>
<body style='margin:0; padding:0; background:#f1f5f9; font-family:Segoe UI, Arial, sans-serif;'>

    <div style='max-width:650px; margin:30px auto; background:#ffffff; border-radius:18px; overflow:hidden; box-shadow:0 15px 35px rgba(15,23,42,0.12);'>

        <div style='background:linear-gradient(135deg,#0db6c9,#20c96b); padding:28px 32px; color:#ffffff;'>
            <h2 style='margin:0; font-size:26px;'>Sistema de Tickets</h2>
            <p style='margin:8px 0 0 0; font-size:14px;'>Credenciales de acceso</p>
        </div>

        <div style='padding:32px;'>

            <h3 style='margin:0 0 15px 0; color:#0f172a; font-size:23px;'>Hola " + WebUtility.HtmlEncode(usuario.NombreCompleto) + @"</h3>

            <p style='color:#334155; font-size:15px; line-height:1.6;'>
                Se ha creado tu usuario en el Sistema de Tickets.
            </p>

            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:14px; padding:20px; margin:24px 0;'>

                <p><b>Usuario:</b> " + WebUtility.HtmlEncode(usuario.NombreUsuario) + @"</p>
                <p><b>Contraseña temporal:</b> " + WebUtility.HtmlEncode(passwordTemporal) + @"</p>

            </div>

            <p style='color:#334155; font-size:15px; line-height:1.6;'>
                Por seguridad, al ingresar por primera vez el sistema te solicitará cambiar tu contraseña.
            </p>

            <p style='margin-top:24px;'>
                <a href='" + loginUrl + @"'
                   style='display:inline-block; background:#0db6c9; color:#ffffff; text-decoration:none; padding:12px 22px; border-radius:12px; font-weight:700;'>
                    Iniciar sesión
                </a>
            </p>

            <p style='color:#64748b; font-size:13px; line-height:1.5; margin-top:26px;'>
                Si no solicitaste este acceso, por favor comunícate con el administrador del sistema.
            </p>

        </div>
    </div>

</body>
</html>";

            return EmailService.EnviarCorreo(
                usuario.Email,
                asunto,
                cuerpo,
                out error
            );
        }

        private string GenerarNombreUsuario(string email, string nombreContacto)
        {
            string baseUsuario = "";

            if (!string.IsNullOrWhiteSpace(email) && email.Contains("@"))
            {
                baseUsuario = email.Split('@')[0];
            }
            else
            {
                baseUsuario = nombreContacto;
            }

            baseUsuario = NormalizarTexto(baseUsuario);

            if (string.IsNullOrWhiteSpace(baseUsuario))
            {
                baseUsuario = "usuario";
            }

            string usuarioFinal = baseUsuario;
            int contador = 1;

            while (db.Usuarios.Any(u => u.NombreUsuario == usuarioFinal))
            {
                usuarioFinal = baseUsuario + contador;
                contador++;
            }

            return usuarioFinal;
        }

        private string GenerarPasswordTemporal()
        {
            string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string minusculas = "abcdefghijkmnopqrstuvwxyz";
            string numeros = "23456789";
            string simbolos = "!@$?";

            Random random = new Random(Guid.NewGuid().GetHashCode());

            char[] password = new char[10];

            password[0] = mayusculas[random.Next(mayusculas.Length)];
            password[1] = minusculas[random.Next(minusculas.Length)];
            password[2] = numeros[random.Next(numeros.Length)];
            password[3] = simbolos[random.Next(simbolos.Length)];

            string todos = mayusculas + minusculas + numeros + simbolos;

            for (int i = 4; i < password.Length; i++)
            {
                password[i] = todos[random.Next(todos.Length)];
            }

            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        private IXLWorksheet BuscarHoja(XLWorkbook workbook, params string[] nombres)
        {
            foreach (string nombre in nombres)
            {
                IXLWorksheet hoja = workbook.Worksheets
                    .FirstOrDefault(x => NormalizarTexto(x.Name) == NormalizarTexto(nombre));

                if (hoja != null)
                {
                    return hoja;
                }
            }

            return null;
        }

        private Dictionary<string, int> ObtenerColumnas(IXLWorksheet hoja)
        {
            Dictionary<string, int> columnas = new Dictionary<string, int>();

            foreach (IXLCell celda in hoja.Row(1).CellsUsed())
            {
                string nombre = NormalizarTexto(celda.GetString());

                if (!columnas.ContainsKey(nombre))
                {
                    columnas.Add(nombre, celda.Address.ColumnNumber);
                }
            }

            return columnas;
        }

        private string LeerCelda(
            IXLWorksheet hoja,
            Dictionary<string, int> columnas,
            int fila,
            params string[] nombresColumnas)
        {
            foreach (string nombreColumna in nombresColumnas)
            {
                string clave = NormalizarTexto(nombreColumna);

                if (columnas.ContainsKey(clave))
                {
                    return hoja.Cell(fila, columnas[clave]).GetString().Trim();
                }
            }

            return "";
        }

        private bool LeerBooleano(
            IXLWorksheet hoja,
            Dictionary<string, int> columnas,
            int fila,
            bool valorPorDefecto,
            params string[] nombresColumnas)
        {
            string valor = LeerCelda(hoja, columnas, fila, nombresColumnas);

            if (string.IsNullOrWhiteSpace(valor))
            {
                return valorPorDefecto;
            }

            string valorNormalizado = NormalizarTexto(valor);

            if (valorNormalizado.Contains("si") ||
                valorNormalizado.Contains("true") ||
                valorNormalizado.Contains("activo") ||
                valor.Contains("✓") ||
                valorNormalizado == "1")
            {
                return true;
            }

            if (valorNormalizado.Contains("no") ||
                valorNormalizado.Contains("false") ||
                valorNormalizado.Contains("inactivo") ||
                valorNormalizado == "0")
            {
                return false;
            }

            return valorPorDefecto;
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "";
            }

            texto = texto.Trim().ToLower();

            string normalizado = texto.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalizado)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);

                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "");
        }

        private void AsignarTextoSiExiste(object objeto, string valor, params string[] nombresPropiedad)
        {
            if (objeto == null || string.IsNullOrWhiteSpace(valor))
            {
                return;
            }

            Type tipo = objeto.GetType();

            foreach (string nombre in nombresPropiedad)
            {
                PropertyInfo propiedad = tipo.GetProperty(nombre);

                if (propiedad == null || !propiedad.CanWrite)
                {
                    continue;
                }

                if (propiedad.PropertyType == typeof(string))
                {
                    propiedad.SetValue(objeto, valor.Trim(), null);
                    return;
                }
            }
        }

        private void AsignarBooleanoSiExiste(object objeto, bool valor, params string[] nombresPropiedad)
        {
            if (objeto == null)
            {
                return;
            }

            Type tipo = objeto.GetType();

            foreach (string nombre in nombresPropiedad)
            {
                PropertyInfo propiedad = tipo.GetProperty(nombre);

                if (propiedad == null || !propiedad.CanWrite)
                {
                    continue;
                }

                if (propiedad.PropertyType == typeof(bool))
                {
                    propiedad.SetValue(objeto, valor, null);
                    return;
                }

                if (propiedad.PropertyType == typeof(bool?))
                {
                    propiedad.SetValue(objeto, valor, null);
                    return;
                }
            }
        }

        private string ObtenerValorTexto(object objeto, params string[] nombresPropiedad)
        {
            if (objeto == null)
            {
                return "";
            }

            Type tipo = objeto.GetType();

            foreach (string nombre in nombresPropiedad)
            {
                PropertyInfo propiedad = tipo.GetProperty(nombre);

                if (propiedad == null)
                {
                    continue;
                }

                object valor = propiedad.GetValue(objeto, null);

                if (valor != null && !string.IsNullOrWhiteSpace(valor.ToString()))
                {
                    return valor.ToString().Trim();
                }
            }

            return "";
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