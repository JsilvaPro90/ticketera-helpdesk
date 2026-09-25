namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarCambioPasswordUsuario : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuarios", "RequiereCambioPassword", c => c.Boolean(nullable: false));
            AddColumn("dbo.Usuarios", "FechaUltimoCambioPassword", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Usuarios", "FechaUltimoCambioPassword");
            DropColumn("dbo.Usuarios", "RequiereCambioPassword");
        }
    }
}
