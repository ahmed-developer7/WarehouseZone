using DevExpress.DataProcessing;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace WMS.Controllers.WebAPI
{
    public class ApiOrdersSyncController : BaseApiController
    {

        private readonly ILookupServices _lookupService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IProductServices _productService;

        public ApiOrdersSyncController(ITerminalServices terminalServices, IPurchaseOrderService purchaseOrderService,
            ITenantLocationServices tenantLocationServices, IOrderService orderService,
            IProductServices productServices, IUserService userService, ILookupServices lookupService, IProductServices productService) :
            base(terminalServices, tenantLocationServices, orderService, productServices, userService)
        {
            _lookupService = lookupService;
            _purchaseOrderService = purchaseOrderService;
            _productService = productService;
        }

        // GET http://ganetest.qsrtime.net/api/sync/orders/{reqDate}/{serialNo}
        // GET http://ganetest.qsrtime.net/api/sync/orders/2014-11-23/920013c000814
        public IHttpActionResult GetOrders(DateTime reqDate, string serialNo)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            reqDate = TerminalServices.GetTerminalSyncDate(reqDate, terminal.TenantId);

            var result = new OrdersSyncCollection();

            var allOrders = OrderService.GetAllOrders(terminal.TenantId, terminal.WarehouseId, true, reqDate, true).ToList();
            var warehouses = _lookupService.GetAllWarehousesForTenant(terminal.TenantId);
            var orders = new List<OrdersSync>();

            foreach (var p in allOrders)
            {
                var order = new OrdersSync();
                var mapped = AutoMapper.Mapper.Map(p, order);
                mapped.TransferWarehouseName = warehouses.FirstOrDefault(x => x.WarehouseId == mapped.TransferWarehouseId)?.WarehouseName;
                orders.Add(mapped);
            }

            result.Count = orders.Count;
            result.TerminalLogId = TerminalServices.CreateTerminalLog(reqDate, terminal.TenantId, orders.Count, terminal.TerminalId, TerminalLogTypeEnum.OrdersSync).TerminalLogId;
            result.Orders = orders;
            return Ok(AutoMapper.Mapper.Map(result, new OrdersSyncCollection()));
        }

        // GET http://ganetest.qsrtime.net/api/sync/order-status/{serialNo}
        // GET http://ganetest.qsrtime.net/api/sync/order-status/920013c000814
        [HttpGet]
        public IHttpActionResult UpdateOrderStatus(string serialNo, int orderId, int statusId)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }
            var result = OrderService.UpdateOrderStatus(orderId, statusId, 0);
            return Ok(AutoMapper.Mapper.Map(result, new OrdersSync()));
        }

        [HttpPost]
        public IHttpActionResult GoodsReturn(GoodsReturnRequestSync model)
        {
            var terminal = TerminalServices.GetTerminalBySerial(model.SerialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(model.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            bool proccess = true;
            var result = AutoMapper.Mapper.Map(model, new GoodsReturnResponse());
            var product = _productService.GetProductMasterById(model.ProductId);
            if (product != null)
            {
                if (model.userId > 0)
                {
                    if (!product.Serialisable && !product.ProcessByPallet)
                    {
                        model.OrderId = Inventory.StockTransaction(model, null, null);

                    }
                    if (product.Serialisable)
                    {
                        if (model.ProductSerials != null || model.ProductSerials.Count > 0)
                        {
                            model.OrderId = Inventory.StockTransaction(model, null, null, null);
                        }
                        else
                        {
                            model.OrderId = -1;
                        }
                    }
                    if (product.ProcessByPallet)
                    {
                        if (model.InventoryTransactionType.HasValue && (model.InventoryTransactionType == (int)InventoryTransactionTypeEnum.PurchaseOrder || model.InventoryTransactionType == (int)InventoryTransactionTypeEnum.SalesOrder))
                        {
                            proccess = false;
                        }
                        model.OrderId = _purchaseOrderService.ProcessPalletTrackingSerial(model, null, proccess);
                    }

                    if (model.OrderId >= 0)
                    {
                        result.IsSuccess = true;
                        result.CanProceed = true;
                        result.orderId = model.OrderId;
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.CanProceed = false;
                        result.FailureMessage = "Serialisable products must have serials along with stock.";
                    }


                }
                else
                {
                    result.FailureMessage = "User Id must be greater than 0.";
                }
            }
            else { result.FailureMessage = "Product not found."; }


            return Ok(result);
        }
    }
}