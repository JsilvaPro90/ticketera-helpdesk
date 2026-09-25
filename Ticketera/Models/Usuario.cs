using System;
using System.ComponentModel.DataAnnotations;

namespace Ticketera.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string NombreUsuario { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; }

        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(30)]
        public string Rol { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string ResetToken { get; set; }

        public DateTime? ResetTokenExpira { get; set; }

        public int? ContactoId { get; set; }

        public int? CreadoPorUsuarioId { get; set; }

        public bool RequiereCambioPassword { get; set; }

        public DateTime? FechaUltimoCambioPassword { get; set; }

        public virtual Contacto Contacto { get; set; }
    }
}