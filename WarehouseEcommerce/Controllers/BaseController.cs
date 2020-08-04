﻿using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Web.Mvc;


namespace WarehouseEcommerce.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ICoreOrderService OrderService;
        protected readonly IPropertyService PropertyService;
        protected readonly IAccountServices AccountServices;
        protected readonly ILookupServices LookupServices;
        public static string NoImage = "/UploadedFiles/Products/no-image.png";
        public static string uploadedProductCategoryfilePath = "/UploadedFiles/ProductCategory/";
        public static int tenantId = Convert.ToInt32(ConfigurationManager.AppSettings["TenantId"]);



        public BaseController(ICoreOrderService orderService, IPropertyService propertyService, IProductServices productServices, IAccountServices accountServices, ILookupServices lookupServices)
        {
            OrderService = orderService;
            PropertyService = propertyService;
            AccountServices = accountServices;
            LookupServices = lookupServices;


        }

        public BaseController(ICoreOrderService orderService, IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices)
        {
            OrderService = orderService;
            PropertyService = propertyService;
            AccountServices = accountServices;
            LookupServices = lookupServices;
        }

        protected TenantLocations CurrentWarehouse
        {
            get
            {
                if (_CurrentWarehouse != null) return _CurrentWarehouse;

                _CurrentWarehouse = caCurrent.CurrentWarehouse();
                return _CurrentWarehouse;
            }
            set { _CurrentWarehouse = value; }
        }

        private TenantLocations _CurrentWarehouse { get; set; }

        protected caTenant CurrentTenant
        {
            get
            {
                if (_CurrentTenant != null) return _CurrentTenant;

                _CurrentTenant = caCurrent.CurrentTenant();

                return _CurrentTenant;
            }
            set { _CurrentTenant = value; }
        }

        private caTenant _CurrentTenant { get; set; }

        private caUser _CurrentUser { get; set; }

        protected caUser CurrentUser
        {
            get
            {
                if (_CurrentUser != null) return _CurrentUser;

                _CurrentUser = caCurrent.CurrentUser();
                return _CurrentUser;
            }
            set { _CurrentUser = value; }
        }

        public int CurrentUserId
        {
            get { return CurrentUser.UserId; }
        }

        public int CurrentTenantId
        {
            get { return CurrentTenant.TenantId; }
        }
        public int CurrentWarehouseId
        {
            get { return CurrentWarehouse.WarehouseId; }
        }

        protected void PrepareDirectory(string virtualDirPath)
        {
            if (!Directory.Exists(Server.MapPath(virtualDirPath)))
            {
                Directory.CreateDirectory(Server.MapPath(virtualDirPath));
            }
        }

        public string GenerateNextOrderNumber(InventoryTransactionTypeEnum type)
        {
            return OrderService.GenerateNextOrderNumber(type, CurrentTenantId);
        }
        public string GenerateNextOrderNumber(string type)
        {
            switch (type)
            {
                case "PO":
                    return GenerateNextOrderNumber(InventoryTransactionTypeEnum.PurchaseOrder);
                case "SO":
                    return GenerateNextOrderNumber(InventoryTransactionTypeEnum.SalesOrder);
                case "WO":
                    return GenerateNextOrderNumber(InventoryTransactionTypeEnum.WorksOrder);
                case "TO":
                    return GenerateNextOrderNumber(InventoryTransactionTypeEnum.TransferIn);
            }
            return GenerateNextOrderNumber(InventoryTransactionTypeEnum.WorksOrder);
        }

        public ActionResult ErrorPage()
        {
            return RedirectToAction("Index", "Error");
        }

        public DayOfWeek GetWeekDay(DateTime date)
        {
            CultureInfo cInfo = CultureInfo.CurrentCulture;

            return cInfo.Calendar.GetDayOfWeek(date);
        }

        public int GetWeekNumber(DateTime date)
        {
            CultureInfo cInfo = CultureInfo.CurrentCulture;

            return cInfo.Calendar.GetWeekOfYear(date, cInfo.DateTimeFormat.CalendarWeekRule, cInfo.DateTimeFormat.FirstDayOfWeek);
        }

        public int GetWeekNumber()
        {
            CultureInfo cInfo = CultureInfo.CurrentCulture;

            return cInfo.Calendar.GetWeekOfYear(DateTime.UtcNow, cInfo.DateTimeFormat.CalendarWeekRule, cInfo.DateTimeFormat.FirstDayOfWeek);
        }

        public DateTime GetDateFromWeekNumberAndDayOfWeek(int weekNumber, int year, int dayOfWeek)
        {
            // current culture info
            CultureInfo cInfo = CultureInfo.CurrentCulture;

            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)cInfo.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;

            DateTime firstMonday = jan1.AddDays(daysOffset);

            int firstWeek = cInfo.Calendar.GetWeekOfYear(jan1, cInfo.DateTimeFormat.CalendarWeekRule, cInfo.DateTimeFormat.FirstDayOfWeek);

            var weekNum = weekNumber;

            if ((firstWeek <= 1 || firstWeek >= 52) && daysOffset >= -3)
            {
                weekNum -= 1;
            }

            int noToAdd = dayOfWeek;
            if (dayOfWeek == 0) noToAdd = 7;

            var result = firstMonday.AddDays(weekNum * 7 + noToAdd - 1);
            return result;
        }

        public List<DateTime> GetWeekDatesList(int week, int year)
        {
            List<DateTime> weekDates = new List<DateTime>();
            int[] weekdaysEnumIds = { 1, 2, 3, 4, 5, 6, 0 };

            foreach (var i in weekdaysEnumIds)
            {
                weekDates.Add(GetDateFromWeekNumberAndDayOfWeek(week, year, i));
            }

            return weekDates;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.TimeZone = GetCurrentTimeZone();
            ViewBag.BaseFilePath = ConfigurationManager.AppSettings["BaseFilePath"];

            var queryString = Request.QueryString["fragment"];
            if (queryString != null && queryString != string.Empty)
            {
                ViewBag.Fragment = queryString;
            }

            base.OnActionExecuting(filterContext);
        }

        public string GetCurrentTimeZone()
        {
            var user = CurrentUser;
            var tenant = CurrentTenant;
            string timeZone = "GMT Standard Time";

            if (tenant != null && !string.IsNullOrEmpty(tenant.TenantTimeZoneId))
            {
                timeZone = tenant.TenantTimeZoneId;
            }

            if (user != null && !string.IsNullOrEmpty(user.UserTimeZoneId))
            {
                timeZone = user.UserTimeZoneId;
            }

            return timeZone;

        }

        public void VerifyOrderStatus(int orderId)
        {
            var order = OrderService.GetOrderById(orderId);
            if (order.OrderStatusID == (int)OrderStatusEnum.Active)
            {
                ViewBag.PreventProcessing = false;

            }
            else
            {
                ViewBag.PreventProcessing = true;
                ViewBag.Error = "This order is on hold and cannot be processed";
            }


        }


        public string GetPathAgainstProductId(int productId, bool status = false)
        {
            string paths = "";
            var _productServices = DependencyResolver.Current.GetService<IProductServices>();
            //string[] imageFormats = ConfigurationManager.AppSettings["ImageFormats"].Split(new char[] { ',' });
            //bool defaultimage = false; bool hover = false;
            //if (status) { defaultimage = true; } else { hover = true; };

            //var path = _productServices.GetProductFiles(productId, tenantId, defaultimage: defaultimage, hover: hover).ToList();
            //if (path.Count > 0)
            //{

            //    var data = from files in path.Where(a => imageFormats.Contains(new DirectoryInfo(a.FilePath).Extension, StringComparer.CurrentCultureIgnoreCase))
            //               select new
            //               {
            //                   FileUrl = files.FilePath

            //               };
            //    if (data != null)
            //    {
            //        paths = data.FirstOrDefault()?.FileUrl;
            //    }
            //    else
            //    {
            //        paths = NoImage;
            //    }


            //}

            return paths;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }
}