using System.Data.Entity;

namespace Ticketera.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Entidad> Entidades { get; set; }
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<TicketAdjunto> TicketAdjuntos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Una empresa puede tener muchos contactos
            modelBuilder.Entity<Contacto>()
                .HasRequired(c => c.Entidad)
                .WithMany(e => e.Contactos)
                .HasForeignKey(c => c.EntidadId)
                .WillCascadeOnDelete(false);

            // Una empresa puede tener muchos tickets
            modelBuilder.Entity<Ticket>()
                .HasRequired(t => t.Entidad)
                .WithMany(e => e.Tickets)
                .HasForeignKey(t => t.EntidadId)
                .WillCascadeOnDelete(false);

            // Un ticket pertenece a un contacto solicitante
            modelBuilder.Entity<Ticket>()
                .HasRequired(t => t.Contacto)
                .WithMany()
                .HasForeignKey(t => t.ContactoId)
                .WillCascadeOnDelete(false);

            // Un ticket puede estar asignado a una persona interna de soporte
            modelBuilder.Entity<Ticket>()
                .HasOptional(t => t.AsignadoA)
                .WithMany()
                .HasForeignKey(t => t.AsignadoAId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TicketAdjunto>()
            .HasRequired(a => a.Ticket)
            .WithMany(t => t.Adjuntos)
            .HasForeignKey(a => a.TicketId)
            .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }
}