using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Services;
using PagedList;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace WarehouseEcommerce.Controllers
{
    public class ProductsController : BaseController
    {

        private readonly IUserService _userService;
        private readonly IActivityServices _activityServices;
        private readonly ITenantsServices _tenantServices;
        private readonly IProductLookupService _productlookupServices;
        private readonly ILookupServices _lookupServices;
        private readonly IProductServices _productServices;

        public ProductsController(IProductServices productServices, IProductLookupService productlookupServices, ICoreOrderService orderService, IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices, IUserService userService, IActivityServices activityServices, ITenantsServices tenantServices)
            : base(orderService, propertyService, accountServices, lookupServices)
        {
            _userService = userService;
            _activityServices = activityServices;
            _tenantServices = tenantServices;
            _productlookupServices = productlookupServices;
            _lookupServices = lookupServices;
            _productServices = productServices;
        }
        // GET: Products

        public ActionResult ProductCategories(int? productGroupId, int? sortOrder, string currentFilter, string searchString, int? page, int? pagesize = 10)
        {
            ViewBag.groupId = productGroupId;
            ViewBag.CurrentSort = sortOrder;
            //ViewBag.ProductPath = uploadedProductfilePath;
            ViewBag.SortedValues = (sortOrder ?? 1);
            ViewBag.pageList = new SelectList(from d in Enumerable.Range(1, 5) select new SelectListItem { Text = (d * 10).ToString(), Value = (d * 10).ToString() }, "Value", "Text", pagesize);
            ViewBag.searchString = searchString;
            var product = _productlookupServices.GetAllValidProductGroupById(productGroupId);
            if (!string.IsNullOrEmpty(searchString))
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;
            if (!string.IsNullOrEmpty(searchString))
            {
                product = product.Where(s => s.Name.Contains(searchString));
            }
            switch ((SortProductTypeEnum)(sortOrder ?? 1))
            {
                case SortProductTypeEnum.PriceByDesc:
                    product = product.OrderByDescending(s => s.SellPrice);
                    break;
                case SortProductTypeEnum.NameByAsc:
                    product = product.OrderBy(s => s.Name);
                    break;
                case SortProductTypeEnum.PriceByAsc:
                    product = product.OrderBy(s => s.SellPrice);
                    break;
                default:  // Name ascending 
                    product = product.OrderBy(s => s.Name);
                    break;
            }



            int pageSize = pagesize ?? 10;
            int pageNumber = (page ?? 1);
            var pageedlist = product.ToPagedList(pageNumber, pageSize);
            page = pageNumber = pageNumber > pageedlist.PageCount ? pageedlist.PageCount : pageNumber;
            var data = product.ToPagedList((pageNumber == 0 ? 1 : pageNumber), pageSize);
            if (data.Count > 0)
            {
                var prdouctIds = data.Select(u => u.ProductId).ToList();
                ViewBag.ProductFilesList = _productServices.GetProductFilesByTenantId((CurrentTenantId == 0 ? tenantId : CurrentTenantId), true).Where(u => prdouctIds.Contains(u.ProductId)).ToList();
            }
            return View(data);

        }

        public ActionResult ProductDetails(int? productId)
        {
            ViewBag.DetailImagesPath = _productServices.GetProductFiles(productId ?? 0, (CurrentTenantId == 0 ? tenantId : CurrentTenantId), true).ToList();
            var product = _productServices.GetProductMasterById(productId ?? 0);
            return View(product);
        }


        public JsonResult GetProductCategories()
        {

            var ProductCategories = _lookupServices.GetAllValidProductGroups((CurrentTenantId == 0 ? tenantId : CurrentTenantId), 12);
            if (ProductCategories.Count() <= 0 || ProductCategories == null) return Json(false, JsonRequestBehavior.AllowGet);

            var data = (from pac in ProductCategories
                        select new
                        {
                            pac.ProductGroupId,
                            pac.ProductGroup
                        });
            return Json(data.ToList(), JsonRequestBehavior.AllowGet);


        }

        public JsonResult searchProduct(string searchkey)
        {
            var model = (from product in _productlookupServices.GetAllValidProductGroupById(null)
                         where (product.Name.Contains(searchkey.Trim()) || product.SKUCode.Contains(searchkey.Trim()) || product.ManufacturerPartNo.Contains(searchkey.Trim())
                         || product.Description.Contains(searchkey.Trim()) || product.ProductGroup.ProductGroup.Contains(searchkey.Trim()))
                         select new ProductSearchResult
                         {
                             Id = product.ProductId,
                             Name = product.Name,
                             Path = product.ProductFiles.FirstOrDefault().FilePath,


                         }).OrderBy(u => u.Id).Take(10).ToList();

            model.ForEach(x => x.Path = ConfigurationManager.AppSettings["BaseFilePath"] + x.Path);


            return Json(model, JsonRequestBehavior.AllowGet);
        }




    }

    public class ProductSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }
}