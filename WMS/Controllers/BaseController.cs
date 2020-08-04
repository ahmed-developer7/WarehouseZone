﻿using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using WMS.Helpers;

namespace WMS.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ICoreOrderService OrderService;
        protected readonly IPropertyService PropertyService;
        protected readonly IAccountServices AccountServices;
        protected readonly ILookupServices LookupServices;

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
        protected void SetViewBagItems(Order order = null, EnumAccountType accountType = EnumAccountType.All)
        {
            ViewBag.Accounts = new SelectList(AccountServices.GetAllValidAccounts(CurrentTenantId, accountType), "AccountID", "AccountNameCode");

            var Tenant = caCurrent.CurrentTenant();

            if (Tenant != null)
            {
                var tenantMainLocation = new PropertyContactInfo() { PropertyCode = "TenantLocMain", AddressLine1 = Tenant.TenantAddress1, AddressLine2 = Tenant.TenantAddress2, AddressLine3 = Tenant.TenantAddress3, AddressLine4 = Tenant.TenantAddress4, AddressPostcode = Tenant.TenantPostalCode };

                var addressList = new List<PropertyContactInfo>() { tenantMainLocation };

                foreach (var location in Tenant.TenantLocations.OrderBy(m => m.WarehouseName))
                {
                    addressList.Add(new PropertyContactInfo()
                    {
                        AddressLine1 = location.AddressLine1,
                        AddressLine2 = location.AddressLine2,
                        AddressLine3 = location.AddressLine3,
                        AddressLine4 = location.AddressLine4,
                        AddressPostcode = location.PostalCode,
                        PropertyCode = "TenantLoc" + location.WarehouseId
                    });
                }

                ViewBag.TenantAddresses = addressList.Select(m => new SelectListItem() { Text = m.FullAddress(), Value = m.PropertyCode, Selected = m.PropertyCode == "TenantLoc" + order?.ShipmentWarehouseId ? true : false });

                var reportTypes = LookupServices.GetAllReportTypes(CurrentTenantId).ToList();

                var rTypes = reportTypes.Where(x => x.AllowReportType == true).Select(repTypes => new
                {
                    repTypes.ReportTypeId,
                    repTypes.TypeName
                }).ToList();

                var cTypes = reportTypes.Where(x => x.AllowChargeTo == true).Select(repTypes => new
                {
                    repTypes.ReportTypeId,
                    repTypes.TypeName
                }).ToList();

                ViewBag.ReportTypes = new SelectList(rTypes, "ReportTypeId", "TypeName", (order != null ? order.ReportTypeId : null));


                ViewBag.ReportTypeChargeTo = new SelectList(cTypes, "ReportTypeId", "TypeName", (order != null ? order.ReportTypeChargeId : null));
            }

            var properties = PropertyService.GetAllValidProperties();
            ViewBag.PropertyAddresses = new SelectList(properties, "PPropertyId", "FullAddress");
            ViewBag.Properties = new SelectList(properties, "PPropertyId", "FullAddress");
            ViewBag.Properties = new SelectList(properties, "PPropertyId", "FullAddress", order?.AccountAddressId);

            var loans = LookupServices.GetAllValidTenantLoanTypes(CurrentTenantId).Select(ln => new
            {
                LoanID = ln.LoanID,
                LoanName = ln.LoanName + "  -  Loan Days= " + ln.LoanDays
            }).ToList();
            ViewBag.TenantLoanTypes = new SelectList(loans, "LoanID", "LoanName");

            ViewBag.AuthUsers = new SelectList(OrderService.GetAllAuthorisedUsers(CurrentTenantId), "UserId", "UserName", order != null ? order.CreatedBy : CurrentUserId);


            ViewBag.JobTypes = new SelectList(OrderService.GetAllValidJobTypes(CurrentTenantId), "JobTypeId", "Name", order != null && order.JobTypeId > 0 ? order.JobTypeId : null);
            ViewBag.JobSubTypes = new SelectList(OrderService.GetAllValidJobSubTypes(CurrentTenantId), "JobSubTypeId", "Name", order != null && order.JobSubTypeId > 0 ? order.JobSubTypeId : null);

            ViewBag.Departments = new SelectList(LookupServices.GetAllValidTenantDepartments(CurrentTenantId), "DepartmentId", "DepartmentName");
            ViewBag.SLAPs = new SelectList(LookupServices.GetAllValidSlaPriorities(CurrentTenantId), "SLAPriorityId", "Priority");
            ViewBag.AccountContacts = new SelectList(AccountServices.GetAllTopAccountContactsByTenantId(CurrentTenantId), "AccountContactId", "ContactName", order?.AccountContactId);
            ViewBag.AccountContacts = new SelectList(AccountServices.GetAllValidAccountContactsByAccountId(order?.AccountID ?? 0, CurrentTenantId), "AccountContactId", "ContactName", order?.AccountContactId);
            ViewBag.AccountAddress = new SelectList(AccountServices.GetAccountAddress(), "AddressID", "FullAddressText", order?.AccountAddressId);


            ViewBag.CurrentUserName = CurrentUser.UserName;

            if (order != null)
            {
                ViewBag.OrderNotesList = order.OrderNotes.Where(x => x.IsDeleted != true).Select(s => new OrderNotesViewModel()
                {
                    Notes = s.Notes,
                    OrderNoteId = s.OrderNoteId,
                    NotesByName = OrderService.GetAuthorisedUserNameById(s.CreatedBy ?? 0),
                    NotesDate = s.DateCreated
                }).ToList();
                ViewBag.OrderDetails = order.OrderDetails;
                ViewBag.OrderProcesses = order.OrderProcess;
                ViewBag.SupplierID = new SelectList(AccountServices.GetAllValidAccounts(CurrentTenantId, EnumAccountType.Supplier), "AccountID", "CompanyName", order.AccountID);
            }

            var expecMints = new Dictionary<string, string>();

            for (var h = 0; h <= 7; h++)
                for (var m = 0; m <= 45; m += 15)
                {
                    if (h == 0 && m == 0) continue;
                    if (h == 7 && m == 45) continue;
                    expecMints.Add(((h * 60) + m).ToString(), h + " hour " + (m > 0 ? m + " minutes" : ""));
                }

            for (var i = 2; i <= 5; i++) expecMints.Add((TimeSpan.FromDays(i).TotalMinutes).ToString(), i + " days");

            if (order != null && order.OrderID > 0)
            {
                ViewBag.ExpectedHoursList = order.ExpectedHours != null ? new SelectList(expecMints, "Key", "Value", order.ExpectedHours.Value) : new SelectList(expecMints, "Key", "Value");
                var tenants = PropertyService.GetAppointmentRecipientTenants(order.OrderID).ToList();
                if (tenants.Any(i => i != null))
                {

                    List<int> selectedTIds = tenants.Select(m => m.PTenantId).ToList();
                    ViewBag.PropertyTenants = new MultiSelectList(PropertyService.GetAllCurrentTenants(order.PPropertyId ?? 0), "PTenantId", "TenantFullName", selectedTIds);
                    ViewBag.PropertyTenantIds = selectedTIds;
                }

                else
                {
                    ViewBag.PropertyTenants =
                        new MultiSelectList(PropertyService.GetAllCurrentTenants(order.PPropertyId ?? 0),
                            "PTenantId", "TenantFullName");
                }
            }
            else
            {
                ViewBag.ExpectedHoursList = new SelectList(expecMints, "Key", "Value");
            }

        }

        public ActionResult AnchoredOrderIndex(string controller, string action, string fragment)
        {
            if (fragment != null && fragment != string.Empty)
            {
                return Redirect(Url.RouteUrl(new { controller, action }) + "#" + fragment);
            }
            else
            {
                return Redirect(Url.RouteUrl(new { controller, action }));
            }
        }

        public string GetAnchorForInventoryTransactionTypeId(int id)
        {
            switch (id)
            {
                default:
                case (int)InventoryTransactionTypeEnum.PurchaseOrder:
                    return "PO";
                case (int)InventoryTransactionTypeEnum.SalesOrder:
                    return "SO";
                case (int)InventoryTransactionTypeEnum.WorksOrder:
                    return "WO";
                case (int)InventoryTransactionTypeEnum.TransferIn:
                    return "TO";
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
            ViewBag.IsWorksOrdersEnabled = IsWorksOrdersEnabled();
            ViewBag.IsVanSalesEnabled = IsVanSalesEnabled();
            ViewBag.IsEcommerceEnabled = IsEcommerceEnabled();
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

        public bool IsWorksOrdersEnabled()
        {
            var tenant = CurrentTenant;

            if (CurrentTenant.TenantModules != null && CurrentTenant.TenantModules.Select(x => x.ModuleId).Contains((int)TenantModuleEnum.WorksOrder))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsVanSalesEnabled()
        {
            var tenant = CurrentTenant;

            if (CurrentTenant.TenantModules != null && CurrentTenant.TenantModules.Select(x => x.ModuleId).Contains((int)TenantModuleEnum.VanSales))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsEcommerceEnabled()
        {
            var tenant = CurrentTenant;

            if (CurrentTenant.TenantModules != null && CurrentTenant.TenantModules.Select(x => x.ModuleId).Contains((int)TenantModuleEnum.Ecommerce))
            {
                return true;
            }
            else
            {
                return false;
            }
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
                ViewBag.Error = "This order is not active and cannot be processed";
            }


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