namespace Ganedata.Core.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GaneTestDb : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.TenantCurrenciesExRates", new[] { "Tenant_TenantId" });
            AlterColumn("dbo.Account", "AccountCode", c => c.String(nullable: false));
            AlterColumn("dbo.ProductMaster", "SKUCode", c => c.String(nullable: false));
            AlterColumn("dbo.ProductMaster", "Name", c => c.String(nullable: false));
            AlterColumn("dbo.ProductMaster", "BarCode", c => c.String(nullable: false));
            AlterColumn("dbo.ProductAccountCodes", "ProdAccCode", c => c.String(nullable: false));
            AlterColumn("dbo.AuthUsers", "UserName", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Terminals", "TermainlSerial", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.LocationGroups", "Locdescription", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.LocationTypes", "LocTypeName", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.ProductGroups", "ProductGroup", c => c.String(nullable: false));
            AlterColumn("dbo.TenantCurrenciesExRates", "Tenant_TenantId", c => c.Int(nullable: false));
            CreateIndex("dbo.TenantCurrenciesExRates", "Tenant_TenantId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.TenantCurrenciesExRates", new[] { "Tenant_TenantId" });
            AlterColumn("dbo.TenantCurrenciesExRates", "Tenant_TenantId", c => c.Int());
            AlterColumn("dbo.ProductGroups", "ProductGroup", c => c.String());
            AlterColumn("dbo.LocationTypes", "LocTypeName", c => c.String());
            AlterColumn("dbo.LocationGroups", "Locdescription", c => c.String());
            AlterColumn("dbo.Terminals", "TermainlSerial", c => c.String());
            AlterColumn("dbo.AuthUsers", "UserName", c => c.String());
            AlterColumn("dbo.ProductAccountCodes", "ProdAccCode", c => c.String());
            AlterColumn("dbo.ProductMaster", "BarCode", c => c.String());
            AlterColumn("dbo.ProductMaster", "Name", c => c.String());
            AlterColumn("dbo.ProductMaster", "SKUCode", c => c.String());
            AlterColumn("dbo.Account", "AccountCode", c => c.String());
            CreateIndex("dbo.TenantCurrenciesExRates", "Tenant_TenantId");
        }
    }
}
