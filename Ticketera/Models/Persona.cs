using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ticketera.Models
{
    public class Persona
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Email { get; set; }

        public string Rol { get; set; }
    }
}