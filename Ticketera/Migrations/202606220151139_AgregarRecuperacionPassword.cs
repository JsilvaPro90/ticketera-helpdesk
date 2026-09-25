namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarRecuperacionPassword : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuarios", "ResetToken", c => c.String(maxLength: 100));
            AddColumn("dbo.Usuarios", "ResetTokenExpira", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Usuarios", "ResetTokenExpira");
            DropColumn("dbo.Usuarios", "ResetToken");
        }
    }
}
