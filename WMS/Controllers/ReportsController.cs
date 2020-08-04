﻿using DevExpress.XtraPrinting;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraReports.UI;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Entities.Helpers;
using Ganedata.Core.Models;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WMS.Reports;
using WMS.Reports.Designs;

namespace WMS.Controllers
{
    public class ReportsController : BaseReportsController
    {
        readonly ITenantLocationServices _tenantLocationsServices;
        readonly IShiftsServices _shiftsServices;
        readonly IEmployeeShiftsServices _employeeShiftsServices;
        readonly IEmployeeServices _employeeServices;
        readonly IProductServices _productServices;
        readonly IAccountServices _accountServices;
        readonly IMarketServices _marketServices;
        private readonly IPalletingService _palletingService;
        readonly IInvoiceService _invoiceService;

        public ReportsController(ITenantLocationServices tenantLocationsServices, IShiftsServices shiftsServices, IEmployeeShiftsServices employeeShiftsServices, IEmployeeServices employeeServices, ICoreOrderService orderService,
            IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices, IAppointmentsService appointmentsService, IGaneConfigurationsHelper ganeConfigurationsHelper, IEmailServices emailServices, IInvoiceService invoiceService,
            ITenantLocationServices tenantLocationservices, IProductServices productServices, ITenantsServices tenantsServices, IPalletingService palleteServices, IMarketServices marketServices)
            : base(orderService, propertyService, accountServices, lookupServices, appointmentsService, ganeConfigurationsHelper, emailServices, tenantLocationservices, tenantsServices)
        {
            _tenantLocationsServices = tenantLocationsServices;
            _shiftsServices = shiftsServices;
            _employeeShiftsServices = employeeShiftsServices;
            _employeeServices = employeeServices;
            _productServices = productServices;
            _accountServices = accountServices;
            _palletingService = palleteServices;
            _marketServices = marketServices;
            _invoiceService = invoiceService;
        }

        public float JobProgressOutSum = 0;
        public float JobProgressReturnSum = 0;
        List<ExpensivePropertiseTotalsViewModel> ExpensivePropertyDetailTotals = new List<ExpensivePropertiseTotalsViewModel>();

        #region Inventory

        public ActionResult Inventory()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            InventoryReport report = CreateInventoryReport();
            report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }

        private void XrLabel18_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            XRLabel cell = sender as XRLabel;
            DateTime? utc = (DateTime?)cell.Value;
            cell.Text = DateTimeToLocal.Convert(utc, GetCurrentTimeZone()).ToString();
        }

        public InventoryReport CreateInventoryReport()
        {
            InventoryReport InventoryReport = new InventoryReport();
            InventoryReport.paramTenantId.Value = CurrentTenantId;
            InventoryReport.paramWarehouseId.Value = CurrentWarehouseId;

            IEnumerable<ProductMaster> products = _productServices.GetAllValidProductMasters(CurrentTenantId);
            StaticListLookUpSettings setting = (StaticListLookUpSettings)InventoryReport.paramProductId.LookUpSettings;

            foreach (var item in products)
            {
                LookUpValue product = new LookUpValue();
                product.Description = item.NameWithCode;
                product.Value = item.ProductId;
                setting.LookUpValues.Add(product);
            }

            return InventoryReport;
        }

        #endregion

        #region StockValueReport

        public ActionResult StockValueReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            StockValueReport report = CreateStockValueReport();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }


        public StockValueReport CreateStockValueReport()
        {
            StockValueReport StockValueReport = new StockValueReport();
            StockValueReport.paramsTenantId.Value = CurrentTenantId;
            StockValueReport.paramWarehouseId.Value = CurrentWarehouseId;

            IEnumerable<ProductMaster> products = _productServices.GetAllValidProductMasters(CurrentTenantId);
            StaticListLookUpSettings setting = (StaticListLookUpSettings)StockValueReport.paramProductId.LookUpSettings;

            foreach (var item in products)
            {
                LookUpValue product = new LookUpValue();
                product.Description = item.NameWithCode;
                product.Value = item.ProductId;
                setting.LookUpValues.Add(product);
            }

            return StockValueReport;
        }

        #endregion

        #region MarketTotalReport

        public ActionResult MarketTotalReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            MarketTotal report = CreateMarketTotalReport();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }


        public MarketTotal CreateMarketTotalReport()
        {
            MarketTotal marketTotal = new MarketTotal();
            marketTotal.TenantId.Value = CurrentTenantId;
            marketTotal.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            marketTotal.paramEndDate.Value = DateTime.Today;
            MarketListViewModel markets = _marketServices.GetAllValidMarkets(CurrentTenantId);
            var model = LookupServices.GetAllInventoryTransactionTypes().Select(u => new
            {
                u.InventoryTransactionTypeId,
                u.InventoryTransactionTypeName

            }).Where(u => u.InventoryTransactionTypeId == (int)InventoryTransactionTypeEnum.DirectSales || u.InventoryTransactionTypeId == (int)InventoryTransactionTypeEnum.Returns || u.InventoryTransactionTypeId == (int)InventoryTransactionTypeEnum.Exchange).ToList();

            StaticListLookUpSettings setting = (StaticListLookUpSettings)marketTotal.MarketId.LookUpSettings;
            var marketlist = markets.Markets.ToList();
            setting.LookUpValues.AddRange(marketlist.Select(u => new LookUpValue(u.Id, u.Name)));
            StaticListLookUpSettings invtrans = (StaticListLookUpSettings)marketTotal.InventoryTransType.LookUpSettings;
            invtrans.LookUpValues.AddRange(model.Select(u => new LookUpValue(u.InventoryTransactionTypeId, u.InventoryTransactionTypeName)));



            return marketTotal;
        }

        #endregion

        #region LowStockItems

        public ActionResult LowStockItemsReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            LowStockItemsReport report = new LowStockItemsReport();
            StaticListLookUpSettings setting = (StaticListLookUpSettings)report.WarehouseParam.LookUpSettings;
            caTenant tenant = caCurrent.CurrentTenant();
            report.TenantIdParam.Value = tenant.TenantId;
            TenantConfig config = _tenantServices.GetTenantConfigById(CurrentTenantId);
            if (!config.ShowDecimalPoint)
            {
                report.lblQuantity.TextFormatString = "{0:0.##}";
            }
            LookUpValue item = new LookUpValue();
            item.Description = "Select Location";
            item.Value = 0;
            setting.LookUpValues.Add(item);

            IEnumerable<TenantLocations> Warehouses = LookupServices.GetAllWarehousesForTenant(CurrentTenantId);

            foreach (var whs in Warehouses)
            {
                LookUpValue item2 = new LookUpValue();
                item2.Description = whs.WarehouseName;
                item2.Value = whs.WarehouseId;
                setting.LookUpValues.Add(item2);
            }

            return View(report);
        }

        #endregion

        #region PurchaseOrder
        public ActionResult PurchaseOrderPrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PurchaseOrderPrint report = CreatePurchaseOrderPrint(id);
            return View(report);

        }
        public ActionResult PurchaseOrderNotePrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            caTenant tenant = caCurrent.CurrentTenant();
            TenantConfig config = _tenantServices.GetTenantConfigById(tenant.TenantId);

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            POCollectionNotePrint report = CreatePurchaseOrderPrints(id);
            return View(report);

        }

        #endregion

        #region SalesOrder

        public ActionResult SalesOrderPrint(int id = 0, bool directsales = false)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var report = CreateSalesOrderPrint(id);
            if (directsales)
            {
                report.lblheading.Text = "INVOICE";
            }
            else
            {
                report.lblheading.Text = "SALES ORDER";
            }

            return View(report);
        }


        #endregion

        #region TransferOrder

        public ActionResult TransferOrderPrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var report = CreateTransferOrderPrint(id);
            return View(report);
        }

        #endregion

        #region WorksOrderPrint

        public ActionResult WorksOrderPrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var report = CreateWorksOrderPrint(id);
            report.paramTenantId.Value = CurrentTenantId;
            return View(report);
        }

        #endregion

        #region WorksOrderDayPrint

        public ActionResult WorksOrderDayPrint()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            caTenant tenant = caCurrent.CurrentTenant();
            var resources = _employeeServices.GetAllActiveAppointmentResourceses(CurrentTenantId).ToList().Select(x => new SelectListItem() { Text = x.Name, Value = x.ResourceId.ToString() }).ToList();
            resources.Insert(0, new SelectListItem() { Text = "All", Value = "0" });

            var report = CreateWorksOrderDayPrint();

            report.paramTenantId.Value = tenant.TenantId;

            return View(report);
        }

        #endregion

        #region SlaWorksOrder

        public ActionResult SlaWorkOrderReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            var report = GetSlaWorksOrderReport();
            return View(report);
        }

        private SlaWorksOrderReport GetSlaWorksOrderReport()
        {
            var report = new SlaWorksOrderReport();

            var setting = (StaticListLookUpSettings)report.WarehouseParam.LookUpSettings;

            report.TenantIdParam.Value = CurrentTenantId;
            report.FromDate.Value = DateTime.Now.AddDays(-60);
            report.ToDate.Value = DateTime.Now;
            setting.LookUpValues.Add(new LookUpValue(0, "Select Location"));
            var warehouses = _tenantLocationsServices.GetAllTenantLocations(CurrentTenantId).ToList();
            foreach (var whs in warehouses)
            {
                setting.LookUpValues.Add(new LookUpValue(whs.WarehouseId, whs.WarehouseName));
            }
            return report;
        }

        #endregion

        #region WorksOrderExpensiveProperties

        public ActionResult WorksOrderExpensiveProperties()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            var report = new WorkOrderExpensivePropertiesReport();
            TenantConfig config = _tenantServices.GetTenantConfigById(CurrentTenantId);
            if (!config.ShowDecimalPoint)
            {
                report.lblQuantity.TextFormatString = "{0:0.##}";
            }
            PopulateExpensivePropertyReportParams(report);
            return View(report);
        }

        private void PopulateExpensivePropertyReportParams(XtraReport report)
        {
            var allProperties = PropertyService.GetAllValidProperties().ToList();
            WorkOrderExpensivePropertiesReport expensivePropertiesReport = (WorkOrderExpensivePropertiesReport)report;
            StaticListLookUpSettings paramPPropertyIdSettings = (StaticListLookUpSettings)expensivePropertiesReport.paramPPropertyId.LookUpSettings;

            paramPPropertyIdSettings.LookUpValues.AddRange(allProperties.Select(m => new LookUpValue(m.PPropertyId, m.PartAddress)));
            expensivePropertiesReport.paramTenantId.Value = CurrentTenantId;
            expensivePropertiesReport.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            expensivePropertiesReport.paramEndDate.Value = DateTime.Today;
            expensivePropertiesReport.ParametersRequestBeforeShow += expensivePropertiesReport_ParametersRequestBeforeShow;

        }

        private void expensivePropertiesReport_ParametersRequestBeforeShow(object sender, ParametersRequestEventArgs e)
        {
            List<int> propertyIds = new List<int>();

            var property = PropertyService.GetAllValidProperties().Where(x => x.SiteId == 1).ToList();

            foreach (var prop in property)
            {
                propertyIds.Add(prop.PPropertyId);
            }

            e.ParametersInformation[1].Parameter.Value = propertyIds;
        }

        #endregion

        #region ChargeReports

        public ActionResult WorksOrdersChargesReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            var report = GetWorksOrdersChargesReport();

            return View(report);
        }
        private WorksOrdersChargesReport GetWorksOrdersChargesReport()
        {
            WorksOrdersChargesReport chargesReport = new Reports.WorksOrdersChargesReport();
            StaticListLookUpSettings paramChargeTypeSettings = (StaticListLookUpSettings)chargesReport.paramChargeType.LookUpSettings;

            var chargeTypes = LookupServices.GetAllReportTypes(CurrentTenantId).Where(x => x.AllowChargeTo == true);

            // add static lookup values in paramter 
            LookUpValue item = new LookUpValue();
            item.Description = "Select Charge Type";
            item.Value = 0;

            paramChargeTypeSettings.LookUpValues.Add(item);

            paramChargeTypeSettings.LookUpValues.AddRange(chargeTypes.Select(m => new LookUpValue(m.ReportTypeId, m.TypeName)));
            chargesReport.paramTenantId.Value = CurrentTenantId;
            chargesReport.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            chargesReport.paramEndDate.Value = DateTime.Today;

            chargesReport.DataSourceDemanded += ChargesReport_DataSourceDemanded;
            return chargesReport;
        }

        private void ChargesReport_DataSourceDemanded(object sender, EventArgs e)
        {
            WorksOrdersChargesReport report = (WorksOrdersChargesReport)sender;
            int DefaultId = Convert.ToInt32(report.paramChargeType.Value);

            DateTime endDate = (DateTime)report.Parameters["paramEndDate"].Value;
            endDate = endDate.AddHours(24);
            report.Parameters["paramEndDate"].Value = endDate;

            if (DefaultId != 0)
            {
                report.FilterString = "[ReportTypeChargeId] = ?paramChargeType";
            }
        }

        #endregion

        #region TAReports

        public ActionResult ReportListing()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();

            TimeAttendanceStatusReport report = new TimeAttendanceStatusReport();

            PageHeader(report);
            CreateListingReport(report, "Employee");

            report.DataSourceDemanded += Report_DataSourceDemanded;

            Parameter paramTenantId = new Parameter()
            {
                Name = "tenantId",
                Type = typeof(int),
                Value = tenant.TenantId,
                Visible = false
            };

            Parameter paramReportType = new Parameter()
            {
                Name = "ReportTypeId",
                Type = typeof(string),
                Value = "Lateness",
                Description = "Report Type:",
                LookUpSettings = new StaticListLookUpSettings(),
            };

            ((StaticListLookUpSettings)paramReportType.LookUpSettings).LookUpValues.AddRange(new LookUpValue[] {
                new LookUpValue("1", "Lateness"),
                new LookUpValue("2", "On Site"),
                new LookUpValue("3", "Off Site"),
                new LookUpValue("4", "Absence")
            });

            Parameter paramLocations = new Parameter()
            {
                Name = "WarehouseId",
                Type = typeof(string),
                Description = "Location: ",
                Value = "Select a Location",
                LookUpSettings = new DynamicListLookUpSettings()
                {
                    DataSource = _tenantLocationsServices.GetAllTenantLocations(tenant.TenantId),
                    DataMember = String.Empty,
                    DisplayMember = "WarehouseName",
                    ValueMember = "WarehouseId"
                }
            };

            Parameter paramStartDate = new Parameter()
            {
                Name = "StartDate",
                Type = typeof(DateTime),
                Value = DateTime.Now,
                Description = "Start Date:",
            };

            Parameter paramEndDate = new Parameter()
            {
                Name = "EndDate",
                Type = typeof(DateTime),
                Value = DateTime.Now,
                Description = "End Date:",
            };

            Parameter paramGracePeriod = new Parameter()
            {
                Name = "GracePeriodId",
                Type = typeof(string),
                Value = "Select All",
                Description = "Grace Period:",
                LookUpSettings = new StaticListLookUpSettings()
            };

            ((StaticListLookUpSettings)paramGracePeriod.LookUpSettings).LookUpValues.AddRange(new LookUpValue[] {
                new LookUpValue("0", "Select All"),
                new LookUpValue("1", "15 minutes"),
                new LookUpValue("2", "30 minutes"),
                new LookUpValue("3", "1 hour"),
            });

            // Add the parameter to the report.
            report.Parameters.Add(paramReportType);
            report.Parameters.Add(paramLocations);
            report.Parameters.Add(paramStartDate);
            report.Parameters.Add(paramEndDate);
            report.Parameters.Add(paramGracePeriod);
            report.Parameters.Add(paramTenantId);

            return View(report);
        }

        private void Report_DataSourceDemanded(object sender, EventArgs e)
        {
            TimeAttendanceStatusReport report = (TimeAttendanceStatusReport)sender;

            int reportTypeId = (!String.IsNullOrEmpty(report.Parameters["ReportTypeId"].Value.ToString()) ? (int)(report.Parameters["ReportTypeId"].Value.ToString() == "Lateness" ? 1 : Convert.ToInt32(report.Parameters["ReportTypeId"].Value)) : 1); //1=Lateness
            int locationId = (!String.IsNullOrEmpty(report.Parameters["WarehouseId"].Value.ToString()) ? (int)(report.Parameters["WarehouseId"].Value.ToString() == "Select a Location" ? 0 : Convert.ToInt32(report.Parameters["WarehouseId"].Value)) : 0);
            DateTime startDate = (DateTime)report.Parameters["StartDate"].Value;
            DateTime endDate = (DateTime)report.Parameters["EndDate"].Value;
            int tenantId = (int)report.Parameters["tenantId"].Value;
            int gracePeriodId = (!String.IsNullOrEmpty(report.Parameters["GracePeriodId"].Value.ToString()) ? (int)(report.Parameters["GracePeriodId"].Value.ToString() == "Select All" ? 0 : Convert.ToInt32(report.Parameters["GracePeriodId"].Value)) : 0);

            report.FindControl("xrLabel2", true).Text = _tenantLocationsServices.GetTenantLocationById(locationId)?.WarehouseName;
            report.FindControl("xrLabel3", true).Text = $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()}";

            if (reportTypeId == 1)
            {//1=Lateness
                report.FindControl("xrLabel1", true).Text = "Lateness";
                report.DataSource = Lateness(locationId, startDate, endDate, gracePeriodId);
            }

            if (reportTypeId == 2)
            { //2=On Site
                report.FindControl("xrLabel1", true).Text = "On Site";

                report.DataSource = OnSite(locationId, startDate, endDate, tenantId);
            }
            if (reportTypeId == 3)
            { //3=Off Site
                report.FindControl("xrLabel1", true).Text = "Off Site";
                report.DataSource = OffSite(locationId, startDate, endDate, tenantId);
            }

            if (reportTypeId == 4)
            { //4=Absence
                report.FindControl("xrLabel1", true).Text = "Absence";
                report.DataSource = Absence(locationId, startDate, endDate);
            }
        }

        public List<ReportViewModel> Lateness(int locationId, DateTime fromDate, DateTime toDate, int gracePeriod)
        {
            List<ReportViewModel> model = new List<ReportViewModel>();

            //get Shifts by LocationId and Date
            var shiftInfo = _shiftsServices.GetShiftsByLocationIdAndBetweenDates(locationId, fromDate, toDate);

            if (shiftInfo != null && shiftInfo.Count() >= 1)
            {
                int id = 1;

                //foreach employeeId
                foreach (var itemShift in shiftInfo)
                {
                    //get EmployeeShifts by EmployeeId and Date
                    var employeeShifts = _employeeShiftsServices.SearchByEmployeeIdAndDate(itemShift.EmployeeId, (DateTime)itemShift.Date, CurrentTenantId);

                    if (employeeShifts != null && employeeShifts.Count() >= 1)
                    {
                        //get first stamp
                        DateTime? stampFirstIn = employeeShifts.FirstOrDefault()?.TimeStamp;
                        DateTime? stampLastOut = employeeShifts.LastOrDefault()?.TimeStamp;



                        if (stampFirstIn.Value.Equals(stampLastOut)) //not equal               
                        {
                            stampLastOut = null;
                        }

                        //get last stamp
                        foreach (var itemEmployeeShifts in employeeShifts)
                        {
                            if (itemEmployeeShifts.StatusType == "In")
                            {
                                //compare Shifts StartTime with EmployeeShifts TimeStamp
                                DateTime startTime = (DateTime)itemShift.StartTime;
                                DateTime timeStamp = (DateTime)itemEmployeeShifts.TimeStamp;

                                switch (gracePeriod)
                                {
                                    case 1:
                                        if ((timeStamp - startTime).TotalMinutes >= 15) //15 minutes passed
                                        {
                                            model.Add(ReportTypeModel(id, timeStamp, stampFirstIn, stampLastOut, itemShift));
                                            id += 1;
                                        };

                                        break;
                                    case 2:
                                        if ((timeStamp - startTime).TotalMinutes >= 30) //30 minutes passed
                                        {
                                            model.Add(ReportTypeModel(id, timeStamp, stampFirstIn, stampLastOut, itemShift));
                                            id += 1;
                                        }

                                        break;
                                    case 3:
                                        if ((timeStamp - startTime).TotalHours >= 1) //1 hour passed
                                        {
                                            model.Add(ReportTypeModel(id, timeStamp, stampFirstIn, stampLastOut, itemShift));
                                            id += 1;
                                        }

                                        break;

                                    default: //show all
                                        model.Add(ReportTypeModel(id, timeStamp, stampFirstIn, stampLastOut, itemShift));

                                        break;
                                }
                            }
                            break;
                        }
                    }
                }
            }

            return model;
        }

        public List<ReportViewModel> OnSite(int locationId, DateTime fromDate, DateTime toDate, int tenantId)
        {

            List<ReportViewModel> model = new List<ReportViewModel>();

            //get all employees at the location
            var employeeList = _employeeServices.GetAllEmployeesByLocation(tenantId, locationId);

            // get days from date to fromDate

            //foreach employeeId
            foreach (var employee in employeeList)
            {

                int id = 1;
                //get EmployeeShifts by EmployeeId and Date
                var employeeShifts = _employeeShiftsServices.SearchByEmployeeIdAndDate(employee.ResourceId, fromDate, CurrentTenantId);

                if (employeeShifts != null && employeeShifts.Count() >= 1)
                {
                    //First Stamp In
                    var firstTimeStamp = employeeShifts.FirstOrDefault().TimeStamp;
                    var lastStamp = employeeShifts.LastOrDefault().TimeStamp;

                    if (employeeShifts.Count() >= 1)
                    {
                        //check if ODD/EVEN scheme; EVEN = Good, ODD = Bad
                        //only get the ODD scheme, w/c means missing Stamp Out or Employee is still On Site
                        if (employeeShifts.Count() % 2 != 0) //ODD
                        {
                            model.Add(ReportTypeModel(id, null, lastStamp, null, employeeShifts.LastOrDefault()));
                            id = id + 1;
                        }
                    }
                }
            }

            return model;
        }

        public List<ReportViewModel> OffSite(int locationId, DateTime fromDate, DateTime toDate, int tenantId)
        {
            List<ReportViewModel> model = new List<ReportViewModel>();

            //get all employees at the location
            var employeeList = _employeeServices.GetAllEmployeesByLocation(tenantId, locationId);

            // get days from date to fromDate

            //foreach employeeId
            foreach (var employee in employeeList)
            {

                int id = 1;
                //get EmployeeShifts by EmployeeId and Date
                var employeeShifts = _employeeShiftsServices.SearchByEmployeeIdAndDate(employee.ResourceId, fromDate, CurrentTenantId);

                if (employeeShifts != null && employeeShifts.Count() >= 1)
                {
                    //First Stamp In
                    var firstTimeStamp = employeeShifts.FirstOrDefault().TimeStamp;
                    var lastStamp = employeeShifts.LastOrDefault().TimeStamp;

                    if (employeeShifts.Count() >= 1)
                    {
                        //check if ODD/EVEN scheme; EVEN = Good, ODD = Bad
                        //only get the EVEN scheme, w/c means employee is out from site.
                        if (employeeShifts.Count() % 2 != 1) //ODD
                        {
                            model.Add(ReportTypeModel(id, null, null, lastStamp, employeeShifts.LastOrDefault()));
                            id = id + 1;
                        }
                    }
                }
            }

            return model;
        }

        public List<ReportViewModel> Absence(int locationId, DateTime fromDate, DateTime toDate)
        {
            List<ReportViewModel> model = new List<ReportViewModel>();

            //get Shifts by LocationId and Date
            var shiftInfo = _shiftsServices.GetShiftsByLocationIdAndBetweenDates(locationId, fromDate, toDate);

            if (shiftInfo != null && shiftInfo.Count() >= 1)
            {
                int id = 1;

                //foreach employeeId
                foreach (var itemShift in shiftInfo)
                {
                    //get EmployeeShifts by EmployeeId and Date
                    var employeeShifts = _employeeShiftsServices.SearchByEmployeeIdAndDate(itemShift.EmployeeId, (DateTime)itemShift.Date,CurrentTenantId);

                    //if no employeeShifts TimeStamp
                    if (employeeShifts.Count() == 0)
                    {
                        model.Add(ReportTypeModel(id, null, null, null, itemShift));
                    }
                }
            }

            return model;
        }

        ReportViewModel ReportTypeModel(int id, DateTime? timeStamp, DateTime? stampFirstIn, DateTime? stampLastOut, Shifts itemShift)
        {
            return new ReportViewModel()
            {
                Id = id,
                Employee = itemShift.Resources.Name,
                Date = itemShift.Date,
                ShiftStartTime = itemShift.StartTime,
                ShiftEndTime = itemShift.EndTime,
                StampIn = stampFirstIn,
                StampOut = stampLastOut,
                LateTime = (timeStamp != null ? (timeStamp - itemShift.StartTime).Value.TotalMinutes : 0f),
            };
        }

        ReportViewModel ReportTypeModel(int id, DateTime? timeStamp, DateTime? stampFirstIn, DateTime? stampLastOut, ResourceShifts shift)
        {
            return new ReportViewModel()
            {
                Id = id,
                Employee = shift.Resources.Name,
                Date = shift.Date,
                ShiftStartTime = null,
                ShiftEndTime = null,
                StampIn = stampFirstIn,
                StampOut = stampLastOut
            };
        }

        #endregion

        #region DevExpress Report

        private void PageHeader(XtraReport report)
        {

            // ---------Add a header to the listing report---------------
            XRTable tableHeader = new XRTable();
            tableHeader.BeginInit();
            tableHeader.Rows.Add(new XRTableRow());

            //tableHeader.Borders = BorderSide.All;
            tableHeader.BorderColor = Color.DarkGray;
            tableHeader.Font = new Font("Tahoma", 9, System.Drawing.FontStyle.Bold);
            tableHeader.Padding = 10;
            tableHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;

            XRTableCell cellHeader1 = new XRTableCell();
            cellHeader1.Text = "Employee";
            XRTableCell cellHeader2 = new XRTableCell();
            cellHeader2.Text = "Date";
            cellHeader2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            XRTableCell cellHeader3 = new XRTableCell();
            cellHeader3.Text = "Shift Start";
            //cellHeader2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            XRTableCell cellHeader4 = new XRTableCell();
            cellHeader4.Text = "Shift End";
            XRTableCell cellHeader5 = new XRTableCell();
            cellHeader5.Text = "Stamp In";
            XRTableCell cellHeader6 = new XRTableCell();
            cellHeader6.Text = "Stamp Out";
            XRTableCell cellHeader7 = new XRTableCell();
            cellHeader7.Text = "Late Time";

            tableHeader.Rows[0].Cells.AddRange(new XRTableCell[] { cellHeader1, cellHeader2, cellHeader3, cellHeader4, cellHeader5, cellHeader6, cellHeader7 });

            PageHeaderBand phReport = new PageHeaderBand();
            phReport.HeightF = tableHeader.HeightF;
            report.Bands.Add(phReport);
            phReport.Controls.Add(tableHeader);

            // Adjust the table width.
            tableHeader.BeforePrint += tableHeader_BeforePrint;
            tableHeader.EndInit();
        }

        private void CreateListingReport(XtraReport report, string dataMember)
        {
            // Create a detail report band and bind it to data.
            DetailReportBand detailReportBand = new DetailReportBand();
            report.Bands.Add(detailReportBand);
            detailReportBand.DataSource = report.DataSource;
            detailReportBand.DataMember = dataMember;

            //------------ Create the (Report Listing) detail band.--------------
            XRTable tableDetail = new XRTable();
            tableDetail.BeginInit();

            tableDetail.Rows.Add(new XRTableRow());
            tableDetail.Borders = ((DevExpress.XtraPrinting.BorderSide)(BorderSide.Top | BorderSide.Left | BorderSide.Right | BorderSide.Bottom));
            tableDetail.BorderColor = Color.DarkGray;
            tableDetail.Font = new Font("Tahoma", 9);
            tableDetail.Padding = 10;
            tableDetail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;

            XRTableCell cellDetail1 = new XRTableCell();
            cellDetail1.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Employee")});
            cellDetail1.WidthF = 108f;

            XRTableCell cellDetail2 = new XRTableCell();
            cellDetail2.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Date")});
            cellDetail2.EvaluateBinding += DateTime2Formatting_EvaluateBinding;

            XRTableCell cellDetail3 = new XRTableCell();
            cellDetail3.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".ShiftStartTime")});
            //cellDetail3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            cellDetail3.EvaluateBinding += DateTimeFormatting_EvaluateBinding;

            XRTableCell cellDetail4 = new XRTableCell();
            cellDetail4.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".ShiftEndTime")});
            //cellDetail4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            cellDetail4.EvaluateBinding += DateTimeFormatting_EvaluateBinding;

            XRTableCell cellDetail5 = new XRTableCell();
            cellDetail5.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".StampIn")});
            //cellDetail5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            cellDetail5.EvaluateBinding += DateTimeFormatting_EvaluateBinding;

            XRTableCell cellDetail6 = new XRTableCell();
            cellDetail6.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".StampOut")});
            cellDetail6.EvaluateBinding += DateTimeFormatting_EvaluateBinding;

            XRTableCell cellDetail7 = new XRTableCell();
            cellDetail7.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".LateTime")});
            cellDetail7.EvaluateBinding += StringFormatting_EvaluateBinding;

            tableDetail.Rows[0].Cells.AddRange(new XRTableCell[] { cellDetail1, cellDetail2, cellDetail3, cellDetail4, cellDetail5, cellDetail6, cellDetail7 });

            DetailBand detailBand = new DetailBand();
            detailBand.Height = tableDetail.Height;
            detailReportBand.Bands.Add(detailBand);
            detailBand.Controls.Add(tableDetail);

            // Adjust the table width.
            tableDetail.BeforePrint += tableDetail_BeforePrint;
            tableDetail.EndInit();

        }

        private void AdjustTableWidth(XRTable table)
        {
            XtraReport report = table.RootReport;
            table.WidthF = report.PageWidth - report.Margins.Left - report.Margins.Right;
        }

        void tableHeader_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            AdjustTableWidth(sender as XRTable);
        }

        void tableDetail_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            AdjustTableWidth(sender as XRTable);
        }

        private void DateTimeFormatting_EvaluateBinding(object sender, BindingEventArgs e)
        {
            XRTableCell cell = (sender as XRTableCell);

            if (e.Value != null && !String.IsNullOrWhiteSpace(e.Value.ToString()))
            {
                DateTime? value = Convert.ToDateTime(e.Value);
                value = DateTimeToLocal.Convert(value, GetCurrentTimeZone());
                string formattedDate = value.Value.ToString("HH:mm:ss");

                e.Value = formattedDate;
            }
        }
        private void DateTime2Formatting_EvaluateBinding(object sender, BindingEventArgs e)
        {
            XRTableCell cell = (sender as XRTableCell);

            if (e.Value != null && !String.IsNullOrWhiteSpace(e.Value.ToString()))
            {
                DateTime? value = Convert.ToDateTime(e.Value);
                value = DateTimeToLocal.Convert(value, GetCurrentTimeZone());
                string formattedDate = value.Value.ToString("MM/dd/yyyy");

                e.Value = formattedDate;
            }
        }
        private void StringFormatting_EvaluateBinding(object sender, BindingEventArgs e)
        {
            XRTableCell cell = (sender as XRTableCell);

            if (e.Value != null && !String.IsNullOrWhiteSpace(e.Value.ToString()))
            {
                string formattedDate = String.Format("{0:0} minutes", e.Value);

                e.Value = formattedDate;
            }
        }
        #endregion

        #region InvoiceReport
        public ActionResult InvoiceDetails(int? id, string InvoiceMasterIds)
        {
            if (!caSession.AuthoriseSession())
            { return Redirect((string)Session["ErrorUrl"]); }
            if (!string.IsNullOrEmpty(InvoiceMasterIds))
            {

                int[] masterIds = Array.ConvertAll(InvoiceMasterIds.Split(','), Int32.Parse);
                var reports = CreateInvoicePrint(0, masterIds);

                return View(reports);
            }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var report = CreateInvoicePrint(id ?? 0);
            return View(report);

        }


        #endregion


        #region PriceGroup
        public ActionResult PriceGroupReport(int PriceGroupId)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            PriceGroupPrint report = CreatePriceGroup(PriceGroupId);
            return View(report);
        }

        public PriceGroupPrint CreatePriceGroup(int PriceGroupId)
        {
            PriceGroupPrint PriceGroupReport = new PriceGroupPrint();
            PriceGroupReport.TenantId.Value = CurrentTenantId;
            PriceGroupReport.PriceGroupId.Value = PriceGroupId;
            return PriceGroupReport;
        }

        #endregion 






        #region OrderReport

        public ActionResult OrdersReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            OrdersReport report = CreateOrderReport();
            return View(report);
        }

        public OrdersReport CreateOrderReport()
        {
            OrdersReport OrderReport = new OrdersReport();
            OrderReport.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            OrderReport.paramEndDate.Value = DateTime.Today;
            OrderReport.paramTenantId.Value = CurrentTenantId;

            StaticListLookUpSettings transactionTypesSettings = (StaticListLookUpSettings)OrderReport.paramTransactionTypes.LookUpSettings;
            StaticListLookUpSettings propertiesSettings = (StaticListLookUpSettings)OrderReport.paramProperty.LookUpSettings;
            StaticListLookUpSettings accountsSettings = (StaticListLookUpSettings)OrderReport.paramAccountIds.LookUpSettings;

            var transactionTypes = LookupServices.GetAllInventoryTransactionTypes();
            transactionTypesSettings.LookUpValues.AddRange(transactionTypes.Select(m => new LookUpValue(m.InventoryTransactionTypeId, m.InventoryTransactionTypeName)));

            var accounts = _accountServices.GetAllValidAccounts(CurrentTenantId).ToList();
            accountsSettings.LookUpValues.AddRange(accounts.Select(m => new LookUpValue(m.AccountID, m.CompanyName)));

            var properties = PropertyService.GetAllValidProperties().ToList();
            propertiesSettings.LookUpValues.AddRange(properties.Select(m => new LookUpValue(m.PPropertyId, m.AddressLine1)));
            TenantConfig config = _tenantServices.GetTenantConfigById(CurrentTenantId);
            if (!config.ShowDecimalPoint)
            {
                OrderReport.lblQuantity.TextFormatString = "{0:0.##}";
            }

            //OrderReport.DataSourceDemanded += OrderReport_DataSourceDemanded;

            return OrderReport;
        }

        private void OrderReport_DataSourceDemanded(object sender, EventArgs e)
        {
            OrdersReport report = (OrdersReport)sender;
            int[] defaultAccountId = (int[])report.paramAccountIds.Value;
            int[] defaultPropertyId = (int[])report.paramProperty.Value;

            if (defaultAccountId.Count() > 0)
            {
                report.FilterString = "[Ac Account ID] In (?paramAccountIds)";
            }

            if (defaultPropertyId.Count() > 0)
            {
                report.FilterString = "[PProperty Id] In (?paramProperty)";
            }
        }

        #endregion

        #region OrdersTotal

        public ActionResult OrdersTotal()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            OrdersTotal report = CreateOrderTotalReport();
            return View(report);
        }

        public OrdersTotal CreateOrderTotalReport()
        {
            OrdersTotal OrderTotal = new OrdersTotal();
            OrderTotal.paramStartDate.Value = DateTime.Now.AddMonths(-1);
            OrderTotal.paramEndDate.Value = DateTime.Now;
            OrderTotal.paramTenantId.Value = CurrentTenantId;
            StaticListLookUpSettings transactionTypesSettings = (StaticListLookUpSettings)OrderTotal.paramTransactionTypes.LookUpSettings;
            var transactionTypes = LookupServices.GetAllInventoryTransactionTypes();
            transactionTypesSettings.LookUpValues.AddRange(transactionTypes.Select(m => new LookUpValue(m.InventoryTransactionTypeId, m.InventoryTransactionTypeName)));

            //var accounts = _accountServices.GetAllValidAccounts(CurrentTenantId).ToList();
            //accountsSettings.LookUpValues.AddRange(accounts.Select(m => new LookUpValue(m.AccountID, m.CompanyName)));

            //var properties = PropertyService.GetAllValidProperties().ToList();
            //propertiesSettings.LookUpValues.AddRange(properties.Select(m => new LookUpValue(m.PPropertyId, m.AddressLine1)));

            //OrderReport.DataSourceDemanded += OrderReport_DataSourceDemanded;

            return OrderTotal;
        }


        #endregion

        #region WorksOrderKpiReport

        public ActionResult WorksOrderKpiReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();

            WorksOrderKpiReport report = new WorksOrderKpiReport();

            report.paramStartDate.Value = DateTime.Today.AddDays(-15);
            report.paramEndDate.Value = DateTime.Today;
            report.paramTenantId.Value = tenant.TenantId;

            report.DataSourceDemanded += WorksOrderKpi_DataSourceDemanded;

            // binding

            report.Sector.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Sector")});

            report.Logged.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Logged")});

            report.Completed.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Completed")});

            report.Reallocated.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Reallocated")});

            report.Unallocated.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Unallocated")});

            report.OldestJob.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".OldestJob")});

            return View(report);
        }

        private void WorksOrderKpi_DataSourceDemanded(object sender, EventArgs e)
        {
            WorksOrderKpiReport report = (WorksOrderKpiReport)sender;
            DateTime startDate = (DateTime)report.Parameters["paramStartDate"].Value;
            DateTime endDate = (DateTime)report.Parameters["paramEndDate"].Value;
            endDate = endDate.AddHours(24);
            int tenantId = (int)report.Parameters["paramTenantId"].Value;
            var Logged = OrderService.GetAllWorksOrders(tenantId).Where(x => x.DateCreated >= startDate && x.DateCreated < endDate && x.IsDeleted != true);
            var Completed = OrderService.GetAllWorksOrders(tenantId).Where(x => x.DateUpdated >= startDate && x.DateUpdated < endDate && x.OrderStatusID == (int)OrderStatusEnum.Complete && x.IsDeleted != true);
            var jobTypes = LookupServices.GetAllJobTypes(tenantId).ToList();
            var dataSource = new List<WorksorderKpiReportViewModel>();
            foreach (var type in jobTypes)
            {
                var loggedForType = Logged.Where(x => x.JobTypeId == type.JobTypeId);
                var completedForType = Completed.Where(x => x.JobTypeId == type.JobTypeId).Count();
                var reallocated = loggedForType.Where(x => x.OrderStatusID == (int)OrderStatusEnum.ReAllocationRequired).Count();
                var unallocated = loggedForType.Where(x => x.OrderStatusID == (int)OrderStatusEnum.NotScheduled).Count();
                var oldestJob = OrderService.GetAllWorksOrders(tenantId).FirstOrDefault(x => x.OrderStatusID != (int)OrderStatusEnum.Complete && x.JobTypeId == type.JobTypeId)?.DateCreated.ToString("dd/MM/yyyy");

                if (loggedForType.Count() > 0 || completedForType > 0 || reallocated > 0 || unallocated > 0)
                {
                    var sourceItem = new WorksorderKpiReportViewModel();
                    sourceItem.Sector = type.Name;
                    sourceItem.Logged = loggedForType.Count();
                    sourceItem.Completed = completedForType;
                    sourceItem.Reallocated = reallocated;
                    sourceItem.Unallocated = unallocated;
                    sourceItem.OldestJob = oldestJob;
                    dataSource.Add(sourceItem);

                }
            }

            report.JobsLogged.Text = Logged.Count().ToString();
            report.JobsCompleted.Text = Completed.Count().ToString();
            report.DataSource = dataSource;
        }

        #endregion

        #region DeliveryNote
        public ActionResult DeliveryNotePrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            caTenant tenant = caCurrent.CurrentTenant();
            TenantConfig config = _tenantServices.GetTenantConfigById(tenant.TenantId);

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }



            var groupstatus = _tenantServices.GetAllTenantConfig(CurrentTenantId)?.FirstOrDefault(u => u.EnableTimberProperties)?.EnableTimberProperties;
            if (groupstatus == true)
            {
                DileveryReportPrintGroup report = CreateDeliveryNotePrintByGroup(id);
                report.FooterMsg1.Text = config.DnReportFooterMsg1;
                report.FooterMsg2.Text = config.DnReportFooterMsg2;
                return View(report);
            }
            else
            {
                DeliveryNotePrint report = CreateDeliveryNotePrint(id);
                report.FooterMsg1.Text = config.DnReportFooterMsg1;
                report.FooterMsg2.Text = config.DnReportFooterMsg2;
                return View(report);
            }

        }

        #endregion

        #region PalleteReport
        public ActionResult PalleteReportPrint(int dispatchId = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            caTenant tenant = caCurrent.CurrentTenant();
            //var dataSource = _palletingService.GetAllPalletsDispatch(dispatchId);

            if (dispatchId == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PalleteDispatchesReport report = CreatePalleteReport(dispatchId);
            report.PoPictureBox.BeforePrint += PRPictureBox_BeforePrint;


            return View(report);

        }

        private void PRPictureBox_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var picture = (XRPictureBox)sender;
            picture.ImageUrl = ReportLogoPath("dn-logo.png");
        }


        #endregion

        #region GoodsBookInNote
        public ActionResult GoodsBookInNotePrint(int id = 0)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            DeliveryNotePrint report = CreateDeliveryNotePrint(id);
            report.RequestParameters = false;
            report.xrLabel16.Text = "Goods Book In Note";
            report.xrLabel26.Text = "";
            return View(report);

        }

        #endregion

        #region GoodsReceiveNotePrint
        public ActionResult GoodsReceiveNotePrint(Guid id)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            GoodsReceiveNotePrint report = CreateGoodsReceiveNotePrint(id);
            return View(report);

        }

        #endregion

        #region MarketRoutePrint
        public ActionResult MarketRoutePrint(int id)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            MarketRoutePrint report = CreateMarketRoutePrint(id);
            return View(report);

        }

        #endregion

        #region MarketCustomerPrint
        public ActionResult MarketCustomerPrint(int id)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            MarketCustomerReport report = CreateMarketCustomerPrint(id);
            return View(report);

        }

        #endregion

        #region FinancialTransactionReport

        public ActionResult FinancialTransactionReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            FinancialTransactionReport report = CreateFinancialTransactionReport();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }

        public FinancialTransactionReport CreateFinancialTransactionReport()
        {
            FinancialTransactionReport financialreport = new FinancialTransactionReport();
            financialreport.StartDate.Value = DateTime.Today.AddMonths(-1);
            financialreport.EndDate.Value = DateTime.Today;
            StaticListLookUpSettings accountsSettings = (StaticListLookUpSettings)financialreport.AccountId.LookUpSettings;
            var accounts = _accountServices.GetAllValidAccounts(CurrentTenantId).ToList();
            accountsSettings.LookUpValues.AddRange(accounts.Select(m => new LookUpValue(m.AccountID, m.CompanyName)));

            return financialreport;
        }

        #endregion


        #region ProductMovementReport
        public ActionResult ProductMovementReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            ProductMovementPrint report = CreateProductMovementPrint();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }
        public ProductMovementPrint CreateProductMovementPrint()
        {
            ProductMovementPrint productMovement = new ProductMovementPrint();
            productMovement.StartDate.Value = DateTime.Today.AddMonths(-1);
            productMovement.EndDate.Value = DateTime.Today;
            productMovement.lbldate.Text = DateTime.UtcNow.ToShortDateString();
            productMovement.TenantId.Value = CurrentTenantId;
            productMovement.WarehouseId.Value = CurrentWarehouseId;
            IEnumerable<ProductMaster> products = _productServices.GetAllValidProductMasters(CurrentTenantId);
            StaticListLookUpSettings setting = (StaticListLookUpSettings)productMovement.paramProductId.LookUpSettings;

            foreach (var item in products)
            {
                LookUpValue product = new LookUpValue();
                product.Description = item.NameWithCode;
                product.Value = item.ProductId;
                setting.LookUpValues.Add(product);
            }

            IEnumerable<ProductGroups> groups = LookupServices.GetAllValidProductGroups(CurrentTenantId).ToList();
            StaticListLookUpSettings groupSettings = (StaticListLookUpSettings)productMovement.paramProductGroups.LookUpSettings;

            foreach (var grp in groups)
            {
                LookUpValue group = new LookUpValue();
                group.Description = grp.ProductGroup;
                group.Value = grp.ProductGroupId;
                groupSettings.LookUpValues.Add(group);
            }
            return productMovement;
        }
        #endregion


        #region ProductSalesReportbyAccount

        public ActionResult ProductSaleReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            ProductSoldReport report = CreateProductsalePrint();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }


        public ProductSoldReport CreateProductsalePrint()
        {
            ProductSoldReport productMovement = new ProductSoldReport();
            productMovement.StartDate.Value = DateTime.Now.AddMonths(-1);
            productMovement.EndDate.Value = DateTime.Now;
            productMovement.lbldate.Text = DateTime.UtcNow.ToShortDateString();
            StaticListLookUpSettings marketSettings = (StaticListLookUpSettings)productMovement.MarketId.LookUpSettings;
            var accounts = _marketServices.GetAllValidMarkets(CurrentTenantId).Markets.ToList();
            marketSettings.LookUpValues.AddRange(accounts.Select(m => new LookUpValue(m.Id, m.Name)));

            return productMovement;
        }


        #endregion

        #region ProductSalesReportbySKU

        public ActionResult ProductSaleReportBySku()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            ProductSoldBySkuPrint report = CreateProductSoldBySkuPrint();
            //report.xrLabel18.BeforePrint += XrLabel18_BeforePrint;
            return View(report);
        }


        public ProductSoldBySkuPrint CreateProductSoldBySkuPrint()
        {
            ProductSoldBySkuPrint productSoldBySkuPrint = new ProductSoldBySkuPrint();
            productSoldBySkuPrint.StartDate.Value = DateTime.Today.AddMonths(-1);
            productSoldBySkuPrint.EndDate.Value = DateTime.Today;
            productSoldBySkuPrint.lbldate.Text = DateTime.UtcNow.ToShortDateString();
            productSoldBySkuPrint.TenantId.Value = CurrentTenantId;
            productSoldBySkuPrint.WarehouseId.Value = CurrentWarehouseId;

            IEnumerable<ProductMaster> products = _productServices.GetAllValidProductMasters(CurrentTenantId);
            StaticListLookUpSettings setting = (StaticListLookUpSettings)productSoldBySkuPrint.ProductsIds.LookUpSettings;

            foreach (var item in products)
            {
                LookUpValue product = new LookUpValue();
                product.Description = item.NameWithCode;
                product.Value = item.ProductId;
                setting.LookUpValues.Add(product);
            }

            IEnumerable<ProductGroups> groups = LookupServices.GetAllValidProductGroups(CurrentTenantId).ToList();
            StaticListLookUpSettings groupSettings = (StaticListLookUpSettings)productSoldBySkuPrint.paramProductGroups.LookUpSettings;

            foreach (var grp in groups)
            {
                LookUpValue group = new LookUpValue();
                group.Description = grp.ProductGroup;
                group.Value = grp.ProductGroupId;
                groupSettings.LookUpValues.Add(group);
            }
            IEnumerable<TenantDepartments> tenantDepartments = LookupServices.GetAllValidTenantDepartments(CurrentTenantId).ToList();
            StaticListLookUpSettings tenantDepartmentsSettings = (StaticListLookUpSettings)productSoldBySkuPrint.paramProductDepartment.LookUpSettings;

            foreach (var dep in tenantDepartments)
            {
                LookUpValue group = new LookUpValue();
                group.Description = dep.DepartmentName;
                group.Value = dep.DepartmentId;
                tenantDepartmentsSettings.LookUpValues.Add(group);
            }

            return productSoldBySkuPrint;
        }


        #endregion

        #region InvoiceProfitReport

        public ActionResult InvoiceProfitReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();
            InvoiceProfitPrint report = new InvoiceProfitPrint();
            report.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            report.paramEndDate.Value = DateTime.Today;
            report.DataSourceDemanded += ProfitInvoiceReport_DataSourceDemanded;

            // binding

            report.InvoiceNumber.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".InvoiceNumber")});

            report.CompanyName.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".CompanyName")});

            report.Date.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Date")});

            report.NetAmtB.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".NetAmtB")});

            report.NetAmtS.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".NetAmtS")});

            report.Profit.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Profit")});


            return View(report);
        }

        private void ProfitInvoiceReport_DataSourceDemanded(object sender, EventArgs e)
        {
            InvoiceProfitPrint report = (InvoiceProfitPrint)sender;
            DateTime startDate = (DateTime)report.Parameters["paramStartDate"].Value;
            DateTime endDate = (DateTime)report.Parameters["paramEndDate"].Value;
            endDate = endDate.AddHours(24);
            var InvoiceMaster = _invoiceService.GetAllInvoiceMastersWithAllStatus(CurrentTenantId).Where(x => x.InvoiceDate >= startDate && x.InvoiceDate < endDate).ToList();

            var dataSource = new List<InvoiceProfitReportViewModel>();
            foreach (var type in InvoiceMaster)
            {
                var NetAmtB = _invoiceService.GetNetAmtBuying(type.InvoiceMasterId);
                var NetAmtS = _invoiceService.GetNetAmtSelling(type.InvoiceMasterId);
                var sourceItem = new InvoiceProfitReportViewModel();
                sourceItem.InvoiceNumber = type.InvoiceNumber;
                sourceItem.CompanyName = type.Account?.CompanyName ?? "";
                sourceItem.Date = type.DateCreated; ;
                sourceItem.NetAmtB = NetAmtB;
                sourceItem.NetAmtS = NetAmtS;
                sourceItem.Profit = (NetAmtS - NetAmtB);
                dataSource.Add(sourceItem);
            }

            report.TotalNetAmtB.Text = string.Format("{0:0.00}", dataSource.Sum(u => u.NetAmtB));
            report.TotalNetAmtS.Text = string.Format("{0:0.00}", dataSource.Sum(u => u.NetAmtS));
            report.TotalProfit.Text = string.Format("{0:0.00}", dataSource.Sum(u => u.Profit));

            report.DataSource = dataSource;
        }

        #endregion

        #region InvoiceByProduct

        public ActionResult InvoiceByProductReport()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }
            InvoiceByProductReport report = InvoiceByProductReportPrint();
            return View(report);
        }


        public InvoiceByProductReport InvoiceByProductReportPrint()
        {
            InvoiceByProductReport InvoiceByProductReport = new InvoiceByProductReport();
            InvoiceByProductReport.paramStartDate.Value = DateTime.Today.AddMonths(-1);
            InvoiceByProductReport.paramEndDate.Value = DateTime.Today;
            InvoiceByProductReport.TenantID.Value = CurrentTenantId;
            IEnumerable<ProductMaster> products = _productServices.GetAllValidProductMasters(CurrentTenantId);
            StaticListLookUpSettings setting = (StaticListLookUpSettings)InvoiceByProductReport.paramProductsIds.LookUpSettings;

            foreach (var item in products)
            {
                LookUpValue product = new LookUpValue();
                product.Description = item.NameWithCode;
                product.Value = item.ProductId;
                setting.LookUpValues.Add(product);
            }

            IEnumerable<ProductGroups> groups = LookupServices.GetAllValidProductGroups(CurrentTenantId).ToList();
            StaticListLookUpSettings groupSettings = (StaticListLookUpSettings)InvoiceByProductReport.paramProductGroups.LookUpSettings;

            foreach (var grp in groups)
            {
                LookUpValue group = new LookUpValue();
                group.Description = grp.ProductGroup;
                group.Value = grp.ProductGroupId;
                groupSettings.LookUpValues.Add(group);
            }

            return InvoiceByProductReport;
        }


        #endregion

        #region PalletTrackingLabelReport

        public ActionResult PrintLabelReport(string PalletTrackingIds)
        {
            if (!caSession.AuthoriseSession())
            { return Redirect((string)Session["ErrorUrl"]); }
            if (!string.IsNullOrEmpty(PalletTrackingIds))
            {

                int[] palletIds = Array.ConvertAll(PalletTrackingIds.Split(','), Int32.Parse);
                var reports = CreateLabelPrint(palletIds);
                return View(reports);
            }

            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


        }


        public PrintLabelReport CreateLabelPrint(int[] PalletTrackingIds)
        {
            PrintLabelReport PrintLabel = new PrintLabelReport();
            PrintLabel.ParamPalletTrackingId.Value = PalletTrackingIds;
            PrintLabel.RequestParameters = false;
            return PrintLabel;
        }


        #endregion


        #region HolidayReportsPrint

        public ActionResult HolidayReportsPrint()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();
            HolidayReportPrint report = new HolidayReportPrint();
            report.DataSourceDemanded += HolidayReport_DataSourceDemanded;


            report.UserName.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".UserName")});

            report.Date.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".Date")});

            report.FirstYear.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".FirstYear")});

            report.SecondYear.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".SecondYear")});
            report.ThirdYear.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".ThirdYear")});
            report.FourthYear.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".FourthYear")});

            report.FifthYear.DataBindings.AddRange(new XRBinding[] {
                new XRBinding("Text", report.DataSource, ".FifthYear")});



            return View(report);
        }

        private void HolidayReport_DataSourceDemanded(object sender, EventArgs e)
        {
            HolidayReportPrint report = (HolidayReportPrint)sender;
            var years = _employeeServices.GetEmployeeYears(CurrentTenantId);
            if (years.Count > 0)
            {
                report.lblYear1.Text = years[years.Count - years.Count].ToString();
                report.lblYear2.Text = years[years.Count - (years.Count - 1)].ToString();
                report.lblYear3.Text = years[years.Count - (years.Count - 2)].ToString();
                report.lblYear4.Text = years[years.Count - (years.Count - 3)].ToString();
                report.lblYear5.Text = years[years.Count - (years.Count - 4)].ToString();
                var dataSource = new List<HolidayReportViewModel>();
                var EmployeesList = _employeeServices.GetAllEmployees(CurrentTenantId);
                foreach (var item in EmployeesList)
                {
                    var EmployeeName = item.Name;
                    var JobStartDate = item.JobStartDate;
                    var firstYear =string.Format("{0} {1} {2}",_employeeServices.GetHolidaysCountByEmployeeId(item.ResourceId, years[years.Count - years.Count]).ToString(),"out of", item.HolidayEntitlement.ToString());
                    var secondYear = string.Format("{0} {1} {2}", _employeeServices.GetHolidaysCountByEmployeeId(item.ResourceId, years[years.Count - (years.Count-1)]).ToString(), "out of", item.HolidayEntitlement.ToString());
                    var thirdYear = string.Format("{0} {1} {2}", _employeeServices.GetHolidaysCountByEmployeeId(item.ResourceId, years[years.Count - (years.Count-2)]).ToString(), "out of", item.HolidayEntitlement.ToString());
                    var fourthYear = string.Format("{0} {1} {2}", _employeeServices.GetHolidaysCountByEmployeeId(item.ResourceId, years[years.Count - (years.Count-3)]).ToString(), "out of", item.HolidayEntitlement.ToString());
                    var fifthYear = string.Format("{0} {1} {2}", _employeeServices.GetHolidaysCountByEmployeeId(item.ResourceId, years[years.Count - (years.Count-4)]).ToString(), "out of", item.HolidayEntitlement.ToString());
                    var sourceItem = new HolidayReportViewModel();
                    sourceItem.UserName = EmployeeName;
                    sourceItem.Date = JobStartDate;
                    sourceItem.FirstYear = firstYear;
                    sourceItem.SecondYear = secondYear;
                    sourceItem.ThirdYear = thirdYear;
                    sourceItem.FourthYear = fourthYear;
                    sourceItem.FifthYear = fifthYear;
                    dataSource.Add(sourceItem);
                }
              report.DataSource = dataSource;
            }
        }

        #endregion


    }
}
