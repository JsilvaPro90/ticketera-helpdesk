using System.ComponentModel.DataAnnotations;

namespace Ticketera.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; }

        [Required]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarPassword { get; set; }
    }
}