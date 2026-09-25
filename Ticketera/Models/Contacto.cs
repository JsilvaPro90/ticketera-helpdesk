using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketera.Models
{
    public class Contacto
    {
        public Contacto()
        {
            Activo = true;
            FechaRegistro = DateTime.Now;
        }

        public int Id { get; set; }

        [Required]
        [Display(Name = "Empresa")]
        public int EntidadId { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [StringLength(100)]
        [Display(Name = "Cargo")]
        public string Cargo { get; set; }

        [StringLength(100)]
        [Display(Name = "Correo")]
        public string Email { get; set; }

        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Display(Name = "Contacto principal")]
        public bool EsPrincipal { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaRegistro { get; set; }

        [ForeignKey("EntidadId")]
        public virtual Entidad Entidad { get; set; }
    }
}