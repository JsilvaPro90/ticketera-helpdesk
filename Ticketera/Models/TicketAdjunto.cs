using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketera.Models
{
    public class TicketAdjunto
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ticket")]
        public int TicketId { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Nombre original")]
        public string NombreOriginal { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Nombre guardado")]
        public string NombreGuardado { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Ruta")]
        public string RutaArchivo { get; set; }

        [StringLength(100)]
        [Display(Name = "Tipo de contenido")]
        public string ContentType { get; set; }

        [StringLength(20)]
        [Display(Name = "Extensión")]
        public string Extension { get; set; }

        [Display(Name = "Tamaño")]
        public long TamanioBytes { get; set; }

        [Display(Name = "Fecha de subida")]
        public DateTime FechaSubida { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }
    }
}