using System.ComponentModel.DataAnnotations;

namespace Ticketera.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [Display(Name = "Usuario o correo")]
        public string UsuarioOEmail { get; set; }
    }
}