namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarUsuariosLogin : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Usuarios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NombreUsuario = c.String(nullable: false, maxLength: 80),
                        NombreCompleto = c.String(nullable: false, maxLength: 150),
                        Email = c.String(maxLength: 150),
                        PasswordHash = c.String(nullable: false),
                        Rol = c.String(nullable: false, maxLength: 30),
                        Activo = c.Boolean(nullable: false),
                        FechaRegistro = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Usuarios");
        }
    }
}
