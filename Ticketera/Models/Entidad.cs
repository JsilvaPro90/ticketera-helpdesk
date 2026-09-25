using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ticketera.Models
{
    public class Entidad
    {
        public Entidad()
        {
            Contactos = new HashSet<Contacto>();
            Tickets = new HashSet<Ticket>();
            Activo = true;
            FechaRegistro = DateTime.Now;
        }

        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Empresa")]
        public string Nombre { get; set; }

        [StringLength(20)]
        [Display(Name = "RUC")]
        public string Ruc { get; set; }

        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(100)]
        [Display(Name = "Correo")]
        public string Email { get; set; }

        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaRegistro { get; set; }

        public virtual ICollection<Contacto> Contactos { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}