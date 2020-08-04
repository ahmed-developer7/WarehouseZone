namespace Ganedata.Core.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TerminalSyncDaysaddedintenantconfig : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TenantConfigs", "TerminalSyncDays", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TenantConfigs", "TerminalSyncDays");
        }
    }
}
