﻿
using System.Web.Http;

namespace WMS
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects; config.Formatters.Remove(config.Formatters.XmlFormatter);
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            config.Routes.MapHttpRoute("WarehouseSyncTenantApi", "api/warehouse-sync/sync-tenants/{siteId}", new { controller = "ApiWarehouseSync", action = "SyncPTenants", siteId = @"\d+" });
            config.Routes.MapHttpRoute("WarehouseSyncLandlordApi", "api/warehouse-sync/sync-landlords/{siteId}", new { controller = "ApiWarehouseSync", action = "SyncPLandlords", siteId = @"\d+" });
            config.Routes.MapHttpRoute("WarehouseSyncPropertyApi", "api/warehouse-sync/sync-properties/{siteId}", new { controller = "ApiWarehouseSync", action = "SyncPProperties", siteId = @"\d+" });
            config.Routes.MapHttpRoute("EmailSchedulerApi", "api/warehouse-emails/send-notifications", new { controller = "ApiWarehouseSync", action = "SendOutEmailNotificationsFromQueue" });
            config.Routes.MapHttpRoute("StockTakeRecordScanApi", "api/stocktake/record-stockscan", new { controller = "ApiStockTakes", action = "RecordScannedProducts" });
            config.Routes.MapHttpRoute("StockTakeDetailQtyUpdateApi", "api/stocktake/stockdetail-updatequantity", new { controller = "ApiStockTakes", action = "UpdateStockTakeDetailQuantity" });
            config.Routes.MapHttpRoute("StockTakeDetailArchiveApi", "api/stocktake/stockdetail-archive", new { controller = "ApiStockTakes", action = "ArchiveStockTakeDetail" });
            config.Routes.MapHttpRoute("StockTakeCreateProductApi", "api/product/create", new { controller = "ApiStockTakes", action = "CreateProductOnStockTake" });
            config.Routes.MapHttpRoute("ProductInfoBySerial", "api/product/serial-details", new { controller = "ApiOrders", action = "VerifyProductInfoBySerial" });

            //Handheld Api's
            config.Routes.MapHttpRoute("UsersSync", "api/sync/users/{reqDate}/{serialNo}", new { controller = "ApiTerminalUserSync", action = "GetUsers", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("AccountsSync", "api/sync/accounts/{reqDate}/{serialNo}", new { controller = "ApiAccountSync", action = "GetAccounts", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("ProductsSync", "api/sync/products/{reqDate}/{serialNo}", new { controller = "ApiProductSync", action = "GetProducts", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("ProductSerialsSync", "api/sync/product-serials/{reqDate}/{serialNo}", new { controller = "ApiProductSerialSync", action = "GetSerials", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("InventoryStocksSync", "api/sync/inventory-stocks/{reqDate}/{serialNo}", new { controller = "ApiInventoryStockSync", action = "GetInventorystocks", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("StockTakesSync", "api/sync/stocktakes/{reqDate}/{serialNo}", new { controller = "ApiStockTakeSync", action = "GetStockTakes", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("StockTakesStatusSync", "api/sync/stocktake-status/{serialNo}/{stocktakeid}/{statusId}", new { controller = "ApiStockTakeSync", action = "UpdateStocktakeStatus", serialNo = string.Empty, stocktakeid = string.Empty, statusId = string.Empty });
            config.Routes.MapHttpRoute("OrdersSync", "api/sync/orders/{reqDate}/{serialNo}", new { controller = "ApiOrdersSync", action = "GetOrders", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("OrderStatusSync", "api/sync/order-status/{serialNo}/{orderid}/{statusId}", new { controller = "ApiOrdersSync", action = "UpdateOrderStatus", serialNo = string.Empty, orderid = string.Empty, statusId = string.Empty });
            config.Routes.MapHttpRoute("OrderProcessesSync", "api/sync/order-processes/{reqDate}/{serialNo}", new { controller = "ApiOrderProcessesSync", action = "GetOrderProcesses", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("OrderProcessesDetailsSync", "api/sync/post-order-processes", new { controller = "ApiOrderProcessesSync", action = "PostOrderProcesses", serialNo = string.Empty, stocktakeid = string.Empty, statusId = string.Empty });
            config.Routes.MapHttpRoute("PalletsSync", "api/sync/pallets/{reqDate}/{serialNo}", new { controller = "ApiPalletsSync", action = "GetPallets", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("PalletsStatusSync", "api/sync/pallet-status/{serialNo}/{palletId}/{statusId}/{palletToken}", new { controller = "ApiPalletsSync", action = "UpdatePalletStatus", serialNo = string.Empty, palletId = string.Empty, statusId = string.Empty, palletToken = string.Empty });
            config.Routes.MapHttpRoute("PalletsProofsSync", "api/sync/pallet-images/{serialNo}/{palletId}", new { controller = "ApiPalletsSync", action = "UpdatePalletImages", serialNo = string.Empty, palletId = string.Empty, statusId = string.Empty });
            config.Routes.MapHttpRoute("PalletsProductsSync", "api/sync/pallet-products/{serialNo}/{reqDate}", new { controller = "ApiPalletsSync", action = "GetPalletProducts", serialNo = string.Empty, reqDate = string.Empty });
            config.Routes.MapHttpRoute("PalletsDispatchMethodsSync", "api/sync/pallet-dispatchmethods/{serialNo}/{reqDate}", new { controller = "ApiPalletsSync", action = "GetPalletDispatchMethods", serialNo = string.Empty, reqDate = string.Empty });

            config.Routes.MapHttpRoute("AllPalletsDispatchesSync", "api/sync/all-pallet-dispatches/{serialNo}/{reqDate}", new { controller = "ApiPalletsSync", action = "GetPalletDispatches", serialNo = string.Empty, reqDate = string.Empty });

            config.Routes.MapHttpRoute("PalletsUpdateProductsSync", "api/sync/pallet-products-processes/{serialNo}", new { controller = "ApiPalletsSync", action = "UpdatePalletProducts", serialNo = string.Empty });
            config.Routes.MapHttpRoute("PalletsDispatchesSync", "api/sync/pallet-dispatch/{serialNo}/{palletId}", new { controller = "ApiPalletsSync", action = "DispatchPallet", serialNo = string.Empty, palletId = string.Empty, statusId = string.Empty });
            config.Routes.MapHttpRoute("SyncAck", "api/sync/verify-acks/{id}/{count}/{serialNo}", new { controller = "ApiTerminalUserSync", action = "VerifyAcknowlegement", id = string.Empty, count = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncReturns", "api/sync/goods-return", new { controller = "ApiOrdersSync", action = "GoodsReturn" });
            config.Routes.MapHttpRoute("SyncWastage", "api/sync/wastage-return", new { controller = "ApiOrdersSync", action = "Wastage" });
            config.Routes.MapHttpRoute("SyncVehicles", "api/sync/vehicles/{reqDate}/{serialNo}", new { controller = "ApiVanSales", action = "GetAllVehicles", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncRandomJobs", "api/sync/myjobs/{reqDate}/{serialNo}/{userId}", new { controller = "ApiMarkets", action = "GetMyMarketJobs", reqDate = string.Empty, serialNo = string.Empty, userId = string.Empty });
            config.Routes.MapHttpRoute("SyncAcceptJob", "api/sync/myjobs-accept", new { controller = "ApiMarkets", action = "AcceptMarketJobRequest", reqDate = string.Empty, serialNo = string.Empty, userId = string.Empty });
            config.Routes.MapHttpRoute("SyncDeclineJob", "api/sync/myjobs-decline", new { controller = "ApiMarkets", action = "DeclineMarketJobRequest", reqDate = string.Empty, serialNo = string.Empty, userId = string.Empty });
            config.Routes.MapHttpRoute("SyncCompleteJob", "api/sync/myjobs-complete", new { controller = "ApiMarkets", action = "CompleteMarketJobRequest", reqDate = string.Empty, serialNo = string.Empty, userId = string.Empty });
            config.Routes.MapHttpRoute("SyncRoutes", "api/sync/market-routes/{reqDate}/{serialNo}", new { controller = "ApiVanSales", action = "GetAllMarketRoutes", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncMyRoutes", "api/sync/market-myroutes/{reqDate}/{serialNo}", new { controller = "ApiVanSales", action = "GetMyMarketRoutes", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncRouteSchedules", "api/sync/market-schedules/{reqDate}/{serialNo}", new { controller = "ApiVanSales", action = "GetAllMarketSchedules", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncVanChecks", "api/sync/market-vanchecks/{reqDate}/{serialNo}", new { controller = "ApiVanSales", action = "GetAllVehicleCheckLists", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncVanReport", "api/sync/market-vanreport/{serialNo}", new { controller = "ApiVanSales", action = "SaveInspectionReport", serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncHoliday", "api/sync/request-holiday", new { controller = "ApiVanSales", action = "CreateHolidayRequest", serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncTerminalData", "api/sync/terminal-data/{serialNo}", new { controller = "ApiVanSales", action = "GetTerminalMetadata", serialNo = string.Empty });
            config.Routes.MapHttpRoute("SyncProductStock", "api/sync/product-stock/{serialNo}/{productId}/{warehouseId}", new { controller = "ApiInventoryStockSync", action = "GetInventoryStockForProduct", serialNo = string.Empty, productId = string.Empty, warehouseId = string.Empty });
            config.Routes.MapHttpRoute("SyncAddTransactionFile", "api/sync/add-transaction-file", new { controller = "ApiVanSales", action = "AddAccountTransactionFileSync" });
            config.Routes.MapHttpRoute("SyncMyHolidayRequests", "api/sync/my-holidays/{serialNo}/{reqDate}/{userId}", new { controller = "ApiVanSales", action = "GetUserHolidayRequests", serialNo = string.Empty, reqDate = string.Empty, userId = string.Empty });
            config.Routes.MapHttpRoute("SyncVanSalesStockingStock", "api/sync/van-sales-stocking/{tenantId}/{reqDate}", new { controller = "ApiVanSales", action = "TransferReplenishmentsForVans", tenantId = string.Empty, reqDate = string.Empty });
            config.Routes.MapHttpRoute("VanSalesDailyReport", "api/sync/van-cash-report", new { controller = "ApiVanSales", action = "VanSalesDailyReport" });
            config.Routes.MapHttpRoute("SyncSystemConnectionCheck", "api/sync/connection-check/{serialNo}", new { controller = "ApiTerminalUserSync", action = "GetConnectionCheck", serialNo = string.Empty });
            config.Routes.MapHttpRoute("GetTerminalGeoLocation", "api/sync/get-geo-location/{serialNo}", new { controller = "ApiTerminalUserSync", action = "GetTerminalGeoLocations", serialNo = string.Empty });
            config.Routes.MapHttpRoute("PostTerminalGeoLocation", "api/sync/post-geo-location", new { controller = "ApiTerminalUserSync", action = "PostTerminalGeoLocation" });
            config.Routes.MapHttpRoute("GetApiPallettrackingSync", "api/sync/get-pallet-tracking/{reqDate}/{serialNo}", new { controller = "ApiPallettrackingSync", action = "GetPallettracking", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("PostApiPallettrackingSync", "api/sync/post-pallet-tracking", new { controller = "ApiPallettrackingSync", action = "PostPallettracking" });
            config.Routes.MapHttpRoute("TenantPriceGroupSync", "api/sync/tenant-price-groups/{reqDate}/{serialNo}", new { controller = "ApiAccountSync", action = "GetTenantPriceGroups", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("TenantPriceGroupDetailSync", "api/sync/tenant-price-groups-details/{reqDate}/{serialNo}", new { controller = "ApiAccountSync", action = "GetTenantPriceGroupDetails", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("GetOrderReceiveCountSync", "api/sync/get-order-receive-count/{reqDate}/{serialNo}", new { controller = "ApiOrderReceiveCountSync", action = "GetOrderReceiveCount", reqDate = string.Empty, serialNo = string.Empty });
            config.Routes.MapHttpRoute("PostOrderReceiveCountSync", "api/sync/post-order-receive-count", new { controller = "ApiOrderReceiveCountSync", action = "PostOrderReceiveCount" });
            config.Routes.MapHttpRoute("PostAssetLog", "api/sync/post-asset-log", new { controller = "ApiAsset", action = "PostAssetLog" });
            config.Routes.MapHttpRoute("PostDispatchProgress", "api/sync/post-dispatch-progress", new { controller = "ApiPalletsSync", action = "PostDispatchProgress" });

            //
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "TAClockApi",
                routeTemplate: "iclock/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

        }
    }
}
