using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Ticketera.Models
{
    public class Ticket
    {
        public Ticket()
        {
            Adjuntos = new HashSet<TicketAdjunto>();
        }
        
        public int Id { get; set; }

        [Required]
        [Display(Name = "Título")]
        public string Titulo { get; set; }

        [Required]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required]
        [Display(Name = "Entidad")]
        public int EntidadId { get; set; }

        [Required]
        [Display(Name = "Contacto solicitante")]
        public int ContactoId { get; set; }

        [Display(Name = "Fecha de apertura")]
        public DateTime FechaApertura { get; set; }

        [Display(Name = "Fecha de resolución")]
        public DateTime? FechaResolucion { get; set; }

        [Display(Name = "Fecha de cierre")]
        public DateTime? FechaCierre { get; set; }

        [Required]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; }

        [Required]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Required]
        [Display(Name = "Urgencia")]
        public string Urgencia { get; set; }

        [Required]
        [Display(Name = "Preferencia de contacto")]
        public string PreferenciaContacto { get; set; }

        [Display(Name = "Asignado a")]
        public int? AsignadoAId { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; }

        [Display(Name = "Fecha de actualización")]
        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey("EntidadId")]
        public virtual Entidad Entidad { get; set; }

        [ForeignKey("ContactoId")]
        public virtual Contacto Contacto { get; set; }

        [ForeignKey("AsignadoAId")]
        public virtual Persona AsignadoA { get; set; }

        public virtual ICollection<TicketAdjunto> Adjuntos { get; set; }
 
    }
}