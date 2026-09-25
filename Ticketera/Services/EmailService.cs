using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Ticketera.Services
{
    public class EmailService
    {
        public static bool EnviarCorreo(string para, string asunto, string cuerpoHtml, out string error)
        {
            error = "";

            try
            {
                if (string.IsNullOrWhiteSpace(para))
                {
                    error = "El correo destino está vacío.";
                    return false;
                }

                if (!EsCorreoValido(para))
                {
                    error = "El correo destino no tiene un formato válido.";
                    return false;
                }

                string host = ConfigurationManager.AppSettings["SmtpHost"];
                string portTexto = ConfigurationManager.AppSettings["SmtpPort"];
                string user = ConfigurationManager.AppSettings["SmtpUser"];
                string pass = ConfigurationManager.AppSettings["SmtpPass"];
                string from = ConfigurationManager.AppSettings["SmtpFrom"];
                string displayName = ConfigurationManager.AppSettings["SmtpDisplayName"];
                string enableSslTexto = ConfigurationManager.AppSettings["SmtpEnableSsl"];

                if (string.IsNullOrWhiteSpace(host))
                {
                    error = "No se encontró SmtpHost en Web.config.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(portTexto))
                {
                    error = "No se encontró SmtpPort en Web.config.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(user))
                {
                    error = "No se encontró SmtpUser en Web.config.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(pass))
                {
                    error = "No se encontró SmtpPass en Web.config.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(from))
                {
                    error = "No se encontró SmtpFrom en Web.config.";
                    return false;
                }

                int port = int.Parse(portTexto);
                bool enableSsl = bool.Parse(enableSslTexto);

                user = user.Trim();
                from = from.Trim();

                // Quita espacios por seguridad.
                // Gmail muestra la clave como: abcd efgh ijkl mnop
                // Pero aquí debe quedar: abcdefghijklmnop
                pass = pass.Replace(" ", "").Trim();

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = "Sistema de Tickets";
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (MailMessage mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(from, displayName);
                    mensaje.To.Add(new MailAddress(para));
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpoHtml;
                    mensaje.IsBodyHtml = true;
                    mensaje.BodyEncoding = Encoding.UTF8;
                    mensaje.SubjectEncoding = Encoding.UTF8;

                    using (SmtpClient smtp = new SmtpClient(host, port))
                    {
                        smtp.EnableSsl = enableSsl;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(user, pass);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Timeout = 30000;

                        smtp.Send(mensaje);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;

                if (ex.InnerException != null)
                {
                    error += " | Detalle: " + ex.InnerException.Message;
                }

                return false;
            }
        }

        public static void EnviarCorreo(string para, string asunto, string cuerpoHtml)
        {
            string error;
            bool enviado = EnviarCorreo(para, asunto, cuerpoHtml, out error);

            if (!enviado)
            {
                throw new Exception(error);
            }
        }

        private static bool EsCorreoValido(string correo)
        {
            try
            {
                MailAddress mail = new MailAddress(correo);
                return mail.Address == correo;
            }
            catch
            {
                return false;
            }
        }
    }
}