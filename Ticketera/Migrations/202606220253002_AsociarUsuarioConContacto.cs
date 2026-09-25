namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AsociarUsuarioConContacto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuarios", "ContactoId", c => c.Int());
            AddColumn("dbo.Usuarios", "CreadoPorUsuarioId", c => c.Int());
            AlterColumn("dbo.Usuarios", "ResetToken", c => c.String());
            CreateIndex("dbo.Usuarios", "ContactoId");
            AddForeignKey("dbo.Usuarios", "ContactoId", "dbo.Contactoes", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Usuarios", "ContactoId", "dbo.Contactoes");
            DropIndex("dbo.Usuarios", new[] { "ContactoId" });
            AlterColumn("dbo.Usuarios", "ResetToken", c => c.String(maxLength: 100));
            DropColumn("dbo.Usuarios", "CreadoPorUsuarioId");
            DropColumn("dbo.Usuarios", "ContactoId");
        }
    }
}
