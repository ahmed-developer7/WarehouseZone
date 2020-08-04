﻿using Ganedata.Core.Entities.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Ganedata.Core.Entities.Domain
{
    public class Groups : PersistableEntity<int>
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }
    }
}