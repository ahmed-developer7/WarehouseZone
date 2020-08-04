namespace Ganedata.Core.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class productpacksize : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProductMaster", "PackSize", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProductMaster", "PackSize");
        }
    }
}
