﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Ganedata.Warehouse.PropertiesSync.Data.Entities
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Winman_RentIncEntities : DbContext
    {
        public Winman_RentIncEntities()
            : base("name=Winman_RentIncEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Host_Prop_inf> Host_Prop_inf { get; set; }
        public virtual DbSet<Host_Prop_inf2> Host_Prop_inf2 { get; set; }
        public virtual DbSet<Host_Ten_inf> Host_Ten_inf { get; set; }
        public virtual DbSet<Host_Tenants> Host_Tenants { get; set; }
        public virtual DbSet<tblLandlordContractor> tblLandlordContractors { get; set; }
        public virtual DbSet<tblLandlordExpenceRate> tblLandlordExpenceRates { get; set; }
    }
}
