using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ganedata.Core.Services
{
    public class caCurrent
    {
        public static caTenant CurrentTenant()
        {
            caTenant tenant = new caTenant();

            if (HttpContext.Current.Session["caTenant"] != null)
            {
                tenant = (caTenant)HttpContext.Current.Session["caTenant"];
            }
            return tenant;
        }

        public static caUser CurrentUser()
        {
            caUser User = new caUser();

            if (HttpContext.Current.Session["caUser"] != null)
            {
                // get properties of tenant
                User = (caUser)HttpContext.Current.Session["caUser"];
            }

            return User;
        }

        public static TenantLocations CurrentWarehouse()
        {
            var context = DependencyResolver.Current.GetService<IApplicationContext>();

            int CurrentWarehouseId;
            TenantLocations Warehouse = new TenantLocations();

            if (HttpContext.Current.Session["CurrentWarehouseId"] != null)
            {
                CurrentWarehouseId = (int)HttpContext.Current.Session["CurrentWarehouseId"];
                Warehouse = context.TenantWarehouses.FirstOrDefault(e => e.WarehouseId == CurrentWarehouseId && e.IsDeleted != true);
            }
            return Warehouse;
        }
    }
}