using System;
using System.Collections.Generic;
using System.Web.Http;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using Ganedata.Core.Services;

namespace WMS.Controllers.WebAPI
{
    public class ApiStockTakeSyncController : BaseApiController
    {
        private readonly IStockTakeApiService _stockTakeApiService;

        public ApiStockTakeSyncController(ITerminalServices terminalServices, ITenantLocationServices tenantLocationServices, IOrderService orderService,
            IProductServices productServices, IUserService userService, IStockTakeApiService stockTakeApiService) :
            base(terminalServices, tenantLocationServices, orderService, productServices, userService)
        {
            _stockTakeApiService = stockTakeApiService;
        }

        // GET http://localhost:8005/api/sync/stocktakes/{reqDate}/{serialNo}
        // GET http://localhost:8005/api/sync/stocktakes/2014-11-23/920013c000814
        public IHttpActionResult GetStockTakes(DateTime reqDate, string serialNo)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var result = new StockTakeSyncCollection();

            var allstockTakes = _stockTakeApiService.GetAllStockTakes(terminal.TenantId,terminal.WarehouseId, reqDate, true);

            var stockTakes = new List<StockTakeSync>();

            foreach (var p in allstockTakes)
            {
                var stockTake = new StockTakeSync();
                var mapped = AutoMapper.Mapper.Map(p, stockTake);
                mapped.StockTakeStatusId = p.Status;
                mapped.WarehouseName = p.TenantWarehouse.WarehouseName;
                stockTakes.Add(mapped);
            }

            result.Count = stockTakes.Count;
            result.TerminalLogId = TerminalServices
                .CreateTerminalLog(reqDate, terminal.TenantId, stockTakes.Count, terminal.TerminalId,
                    TerminalLogTypeEnum.StockTakeSync).TerminalLogId;
            result.StockTakes = stockTakes;
            return Ok(result);
        }
        // GET http://localhost:8005/api/sync/stocktake-status/{reqDate}/{serialNo}
        // GET http://localhost:8005/api/sync/stocktake-status//2014-11-23/920013c000814
        public IHttpActionResult UpdateStocktakeStatus(string serialNo, int stockTakeId, int statusId)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }
            var result = _stockTakeApiService.UpdateStockTakeStatus(stockTakeId, 0, statusId);
            return Ok(result);
        }
    }
}