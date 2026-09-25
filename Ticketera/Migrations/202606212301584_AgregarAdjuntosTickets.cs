namespace Ticketera.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarAdjuntosTickets : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TicketAdjuntoes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketId = c.Int(nullable: false),
                        NombreOriginal = c.String(nullable: false, maxLength: 255),
                        NombreGuardado = c.String(nullable: false, maxLength: 255),
                        RutaArchivo = c.String(nullable: false, maxLength: 500),
                        ContentType = c.String(maxLength: 100),
                        Extension = c.String(maxLength: 20),
                        TamanioBytes = c.Long(nullable: false),
                        FechaSubida = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Tickets", t => t.TicketId, cascadeDelete: true)
                .Index(t => t.TicketId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TicketAdjuntoes", "TicketId", "dbo.Tickets");
            DropIndex("dbo.TicketAdjuntoes", new[] { "TicketId" });
            DropTable("dbo.TicketAdjuntoes");
        }
    }
}
