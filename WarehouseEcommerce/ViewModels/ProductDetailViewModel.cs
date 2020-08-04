using Ganedata.Core.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WarehouseEcommerce.ViewModels
{
    public class ProductDetailViewModel
    {
        public List<ProductMaster> productMasterList { get; set; }
        public ProductMaster ProductMaster { get; set; }

        public List<ProductFiles> ProductFilesList { get; set; }
        public ProductFiles ProductFiles { get; set; }

    }
}