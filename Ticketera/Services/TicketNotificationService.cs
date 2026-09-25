using System;
using System.Net;

namespace Ticketera.Services
{
    public class TicketNotificationService
    {
        public static bool NotificarCambioEstadoTicket(
            string correoDestino,
            int ticketId,
            string tituloTicket,
            string estadoAnterior,
            string estadoNuevo,
            string categoria,
            string empresa,
            string contacto,
            string tecnicoAsignado,
            string comentario,
            out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                error = "No se encontró un correo destino para enviar la notificación.";
                return false;
            }

            if (string.Equals(estadoAnterior, estadoNuevo, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string asunto;
            string tituloCorreo;
            string mensajePrincipal;

            if (EsEstadoEnAtencion(estadoNuevo))
            {
                asunto = $"Ticket #{ticketId} - Está siendo atendido";
                tituloCorreo = "Tu ticket está siendo atendido";
                mensajePrincipal = "Te informamos que tu ticket ya está siendo revisado por nuestro equipo de soporte.";
            }
            else if (EsEstadoResuelto(estadoNuevo))
            {
                asunto = $"Ticket #{ticketId} - Ha sido resuelto";
                tituloCorreo = "Tu ticket ha sido resuelto";
                mensajePrincipal = "Te informamos que tu ticket fue marcado como resuelto.";
            }
            else if (EsEstadoObservado(estadoNuevo))
            {
                asunto = $"Ticket #{ticketId} - Requiere información adicional";
                tituloCorreo = "Tu ticket requiere información adicional";
                mensajePrincipal = "Te informamos que tu ticket fue observado y puede requerir información adicional para continuar con la atención.";
            }
            else
            {
                asunto = $"Ticket #{ticketId} - Cambio de estado";
                tituloCorreo = "Tu ticket cambió de estado";
                mensajePrincipal = $"Te informamos que tu ticket cambió de estado a: {estadoNuevo}.";
            }

            string cuerpoHtml = ConstruirCorreoHtml(
                ticketId,
                tituloTicket,
                tituloCorreo,
                mensajePrincipal,
                estadoNuevo,
                categoria,
                empresa,
                contacto,
                tecnicoAsignado,
                comentario
            );

            return EmailService.EnviarCorreo(correoDestino, asunto, cuerpoHtml, out error);
        }

        private static bool EsEstadoEnAtencion(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return false;

            estado = estado.Trim().ToLower();

            return estado == "en proceso"
                || estado == "en atención"
                || estado == "atendido"
                || estado == "asignado";
        }

        private static bool EsEstadoResuelto(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return false;

            estado = estado.Trim().ToLower();

            return estado == "resuelto"
                || estado == "cerrado"
                || estado == "finalizado";
        }

        private static bool EsEstadoObservado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return false;

            estado = estado.Trim().ToLower();

            return estado == "observado"
                || estado == "pendiente cliente"
                || estado == "requiere información"
                || estado == "requiere informacion";
        }

        private static string ConstruirCorreoHtml(
            int ticketId,
            string tituloTicket,
            string tituloCorreo,
            string mensajePrincipal,
            string estadoNuevo,
            string categoria,
            string empresa,
            string contacto,
            string tecnicoAsignado,
            string comentario)
        {
            tituloTicket = WebUtility.HtmlEncode(tituloTicket ?? "");
            tituloCorreo = WebUtility.HtmlEncode(tituloCorreo ?? "");
            mensajePrincipal = WebUtility.HtmlEncode(mensajePrincipal ?? "");
            estadoNuevo = WebUtility.HtmlEncode(estadoNuevo ?? "");
            categoria = WebUtility.HtmlEncode(categoria ?? "");
            empresa = WebUtility.HtmlEncode(empresa ?? "");
            contacto = WebUtility.HtmlEncode(contacto ?? "");
            tecnicoAsignado = WebUtility.HtmlEncode(tecnicoAsignado ?? "");
            comentario = WebUtility.HtmlEncode(comentario ?? "");

            string empresaHtml = string.IsNullOrWhiteSpace(empresa)
                ? ""
                : $"<p><b>Empresa:</b> {empresa}</p>";

            string contactoHtml = string.IsNullOrWhiteSpace(contacto)
                ? ""
                : $"<p><b>Contacto:</b> {contacto}</p>";

            string tecnicoHtml = string.IsNullOrWhiteSpace(tecnicoAsignado)
                ? ""
                : $"<p><b>Técnico asignado:</b> {tecnicoAsignado}</p>";

            string comentarioHtml = string.IsNullOrWhiteSpace(comentario)
                ? ""
                : $"<p><b>Comentario:</b> {comentario}</p>";

            return $@"
<html>
<body style='margin:0; padding:0; background:#f1f5f9; font-family:Segoe UI, Arial, sans-serif;'>

    <div style='max-width:650px; margin:30px auto; background:#ffffff; border-radius:18px; overflow:hidden; box-shadow:0 15px 35px rgba(15,23,42,0.12);'>

        <div style='background:linear-gradient(135deg,#0db6c9,#20c96b); padding:28px 32px; color:#ffffff;'>
            <h2 style='margin:0; font-size:26px;'>Sistema de Tickets</h2>
            <p style='margin:8px 0 0 0; font-size:14px;'>Notificación automática de seguimiento</p>
        </div>

        <div style='padding:32px;'>

            <h3 style='margin:0 0 15px 0; color:#0f172a; font-size:24px;'>{tituloCorreo}</h3>

            <p style='color:#334155; font-size:15px; line-height:1.6;'>
                {mensajePrincipal}
            </p>

            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:14px; padding:20px; margin:24px 0;'>

                <p><b>Nro. Ticket:</b> #{ticketId}</p>
                <p><b>Asunto:</b> {tituloTicket}</p>
                <p><b>Categoría:</b> {categoria}</p>

                {empresaHtml}
                {contactoHtml}

                <p>
                    <b>Estado actual:</b>
                    <span style='display:inline-block; padding:6px 12px; border-radius:999px; background:#dcfce7; color:#166534; font-weight:700;'>
                        {estadoNuevo}
                    </span>
                </p>

                {tecnicoHtml}
                {comentarioHtml}

            </div>

            <p style='color:#64748b; font-size:13px; line-height:1.5;'>
                Este correo fue generado automáticamente por el Sistema de Tickets.
            </p>

        </div>
    </div>

</body>
</html>";
        }
    }
}