using Ganedata.Core.Services;
using System.Linq;
using System.Web.Mvc;
using WarehouseEcommerce.ViewModels;

namespace WarehouseEcommerce.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IActivityServices _activityServices;
        private readonly ITenantsServices _tenantServices;
        private readonly ILookupServices _lookupServices;
        private readonly IProductServices _productServices;
        public HomeController(ICoreOrderService orderService, IProductServices productServices, IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices, IUserService userService, IActivityServices activityServices, ITenantsServices tenantServices)
            : base(orderService, propertyService, accountServices, lookupServices)
        {
            _lookupServices = lookupServices;
            _userService = userService;
            _activityServices = activityServices;
            _tenantServices = tenantServices;
            _productServices = productServices;

        }
        public ActionResult Index()
        {
            //if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            //ViewBag.TopProduct = _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 12, TopProduct:true).ToList();
            //ViewBag.OnSaleProduct= _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 6, OnSaleProduct: true).ToList();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public PartialViewResult _VerticalNavBarPartial()
        {

            var ProductCategories = _lookupServices.GetAllValidProductGroups((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 12);
            return PartialView(ProductCategories);
        }

        public PartialViewResult _SpecialProductPartial()
        {
            var specialProduct = new ProductDetailViewModel
            {
                productMasterList = _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 6, SpecialProduct: true).ToList(),

            };
            if (specialProduct.productMasterList != null)
            {
                var prdouctIds = specialProduct.productMasterList.Select(u => u.ProductId).ToList();
                specialProduct.ProductFilesList = _productServices.GetProductFilesByTenantId((CurrentTenantId == 0 ? tenantId : CurrentTenantId)).Where(u => prdouctIds.Contains(u.ProductId)).ToList();
            }

            return PartialView(specialProduct);

        }
        public PartialViewResult _TopProductPartial()
        {
            var TopProduct = new ProductDetailViewModel
            {
                productMasterList = _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 12, TopProduct: true).ToList(),

            };
            if (TopProduct.productMasterList != null)
            {
                var prdouctIds = TopProduct.productMasterList.Select(u => u.ProductId).ToList();
                TopProduct.ProductFilesList = _productServices.GetProductFilesByTenantId((CurrentTenantId == 0 ? tenantId : CurrentTenantId)).Where(u => prdouctIds.Contains(u.ProductId)).ToList();
            }

            return PartialView(TopProduct);

        }
        public PartialViewResult _OnSalePartial()
        {
            var onsale = new ProductDetailViewModel
            {
                productMasterList = _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 6, OnSaleProduct: true).ToList(),

            };
            if (onsale.productMasterList != null)
            {
                var prdouctIds = onsale.productMasterList.Select(u => u.ProductId).ToList();
                onsale.ProductFilesList = _productServices.GetProductFilesByTenantId((CurrentTenantId == 0 ? tenantId : CurrentTenantId)).Where(u => prdouctIds.Contains(u.ProductId)).ToList();
            }

            return PartialView(onsale);

        }


        public PartialViewResult _BestSellerPartial()
        {
            var BestSellerProduct = new ProductDetailViewModel
            {
                productMasterList = _productServices.GetProductByCategory((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 2, BestSellerProduct: true).ToList()
            };
            if (BestSellerProduct.productMasterList != null)
            {
                var prdouctIds = BestSellerProduct.productMasterList.Select(u => u.ProductId).ToList();
                BestSellerProduct.ProductFilesList = _productServices.GetProductFilesByTenantId((CurrentTenantId == 0 ? tenantId : CurrentTenantId)).Where(u => prdouctIds.Contains(u.ProductId)).ToList();
            }


            return PartialView(BestSellerProduct);

        }
        public PartialViewResult _NewsLetterPartial()
        {

            return PartialView();

        }
        public PartialViewResult _TopProductBannerPartial()
        {

            return PartialView();

        }
        public PartialViewResult _ImageBlockPartial()
        {

            return PartialView();

        }

        public ActionResult ReturnPath(int productId, bool status)
        {
            string path = GetPathAgainstProductId(productId, status);

            return Content(path);
        }
    }
}