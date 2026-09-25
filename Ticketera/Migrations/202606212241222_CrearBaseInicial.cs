namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CrearBaseInicial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Contactoes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EntidadId = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 120),
                        Cargo = c.String(maxLength: 100),
                        Email = c.String(maxLength: 100),
                        Telefono = c.String(maxLength: 30),
                        EsPrincipal = c.Boolean(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        FechaRegistro = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Entidads", t => t.EntidadId)
                .Index(t => t.EntidadId);
            
            CreateTable(
                "dbo.Entidads",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 150),
                        Ruc = c.String(maxLength: 20),
                        Direccion = c.String(maxLength: 200),
                        Email = c.String(maxLength: 100),
                        Telefono = c.String(maxLength: 30),
                        Activo = c.Boolean(nullable: false),
                        FechaRegistro = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Tickets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Titulo = c.String(nullable: false),
                        Descripcion = c.String(nullable: false),
                        EntidadId = c.Int(nullable: false),
                        ContactoId = c.Int(nullable: false),
                        FechaApertura = c.DateTime(nullable: false),
                        FechaResolucion = c.DateTime(),
                        FechaCierre = c.DateTime(),
                        Tipo = c.String(nullable: false),
                        Categoria = c.String(nullable: false),
                        Estado = c.String(nullable: false),
                        Urgencia = c.String(nullable: false),
                        PreferenciaContacto = c.String(nullable: false),
                        AsignadoAId = c.Int(),
                        FechaCreacion = c.DateTime(nullable: false),
                        FechaActualizacion = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Personas", t => t.AsignadoAId)
                .ForeignKey("dbo.Contactoes", t => t.ContactoId)
                .ForeignKey("dbo.Entidads", t => t.EntidadId)
                .Index(t => t.EntidadId)
                .Index(t => t.ContactoId)
                .Index(t => t.AsignadoAId);
            
            CreateTable(
                "dbo.Personas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(),
                        Email = c.String(),
                        Rol = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Contactoes", "EntidadId", "dbo.Entidads");
            DropForeignKey("dbo.Tickets", "EntidadId", "dbo.Entidads");
            DropForeignKey("dbo.Tickets", "ContactoId", "dbo.Contactoes");
            DropForeignKey("dbo.Tickets", "AsignadoAId", "dbo.Personas");
            DropIndex("dbo.Tickets", new[] { "AsignadoAId" });
            DropIndex("dbo.Tickets", new[] { "ContactoId" });
            DropIndex("dbo.Tickets", new[] { "EntidadId" });
            DropIndex("dbo.Contactoes", new[] { "EntidadId" });
            DropTable("dbo.Personas");
            DropTable("dbo.Tickets");
            DropTable("dbo.Entidads");
            DropTable("dbo.Contactoes");
        }
    }
}
