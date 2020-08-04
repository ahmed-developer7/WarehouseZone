using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using Ganedata.Core.Services;

namespace WMS.Controllers.WebAPI
{
    public class ApiPallettrackingSyncController : BaseApiController
    {
        public ApiPallettrackingSyncController(ITerminalServices terminalServices, ITenantLocationServices tenantLocationServices, IOrderService orderService, IProductServices productServices, IUserService userService) : base(terminalServices, tenantLocationServices, orderService, productServices, userService)
        {

        }

        // call example through URI http://localhost:8005/api/sync/get-pallet-tracking?ReqDate=2014-11-23&SerialNo=920013c000814
        public IHttpActionResult GetPallettracking(DateTime reqDate, string serialNo)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var result = new PalletTrackingSyncCollection();

            var allPallets = ProductServices.GetAllPalletTrackings(terminal.TenantId, terminal.WarehouseId, reqDate).ToList();
            var pallets = new List<PalletTrackingSync>();

            foreach (var p in allPallets)
            {
                var pallet = new PalletTrackingSync();
                var mappedPallet = AutoMapper.Mapper.Map(p, pallet);
                pallets.Add(mappedPallet);
            }

            result.Count = pallets.Count;
            result.TerminalLogId = TerminalServices.CreateTerminalLog(reqDate, terminal.TenantId, pallets.Count(), terminal.TerminalId, TerminalLogTypeEnum.PalletTrackingSync).TerminalLogId;
            result.PalletTrackingSync = pallets;
            return Ok(result);
        }

        // POST http://localhost:8005/api/sync/post-pallet-tracking
        public IHttpActionResult PostPallettracking(PalletTrackingSyncCollection data)
        {
            data.SerialNo = data.SerialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(data.SerialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(data.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            var mobileLocation = TerminalServices.GetMobileLocationByTerminalId(terminal.TerminalId);
            if (mobileLocation != null)
            {
                terminal.WarehouseId = mobileLocation.WarehouseId;
            }

            var results = new PalletTrackingSyncCollection();

            if (data.PalletTrackingSync != null && data.PalletTrackingSync.Any())
            {

                foreach (var item in data.PalletTrackingSync)
                {
                    item.DateUpdated = DateTime.UtcNow;
                    var pallet = ProductServices.SavePalletTrackings(item, terminal.TenantId);

                    results.PalletTrackingSync.Add(pallet);
                    results.Count += 1;
                }
            }

            TerminalServices.CreateTerminalLog(DateTime.UtcNow, terminal.TenantId, data.PalletTrackingSync.Count, terminal.TerminalId, TerminalLogTypeEnum.PostPalletTracking);

            return Ok(results);
        }

    }
}