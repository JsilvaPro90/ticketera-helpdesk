using System;
using System.Collections.Generic;

namespace Ticketera.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int TicketsAbiertos { get; set; }
        public int TicketsEnProceso { get; set; }
        public int TicketsPendientes { get; set; }
        public int TicketsResueltos { get; set; }
        public int TicketsCerrados { get; set; }
        public int TicketsMuyAlta { get; set; }
        public int TicketsSinAsignar { get; set; }

        public List<DashboardItemViewModel> TicketsPorEstado { get; set; }
        public List<DashboardItemViewModel> TicketsPorUrgencia { get; set; }
        public List<DashboardItemViewModel> TicketsPorEmpresa { get; set; }
        public List<DashboardItemViewModel> TicketsPorTecnico { get; set; }
        public List<DashboardTicketViewModel> UltimosTickets { get; set; }
    }

    public class DashboardItemViewModel
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public int Porcentaje { get; set; }
    }

    public class DashboardTicketViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Empresa { get; set; }
        public string Contacto { get; set; }
        public string Tecnico { get; set; }
        public string Estado { get; set; }
        public string Urgencia { get; set; }
        public DateTime FechaApertura { get; set; }
    }
}