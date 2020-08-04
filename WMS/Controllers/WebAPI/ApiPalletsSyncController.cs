using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using Ganedata.Core.Services;

namespace WMS.Controllers.WebAPI
{
    public class ApiPalletsSyncController : BaseApiController
    {
        private readonly IPalletingService _palletService;

        public ApiPalletsSyncController(ITerminalServices terminalServices,
            ITenantLocationServices tenantLocationServices, IOrderService orderService,
            IProductServices productServices, IUserService userService, IPalletingService palletService) :
            base(terminalServices, tenantLocationServices, orderService, productServices, userService)
        {
            _palletService = palletService;
        }

        [HttpGet]
        // GET http://ganetest.qsrtime.net/api/sync/pallets/{reqDate}/{serialNo}
        // GET http://ganetest.qsrtime.net/api/sync/pallets/2014-11-23/920013c000814
        public IHttpActionResult GetPallets(DateTime reqDate, string serialNo)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            reqDate = TerminalServices.GetTerminalSyncDate(reqDate, terminal.TenantId);

            var result = new PalletsSyncCollection();

            var allPallets = _palletService.GetAllPallets(null, null, null, reqDate);
            var palletSyncs = new List<PalletSync>();

            foreach (var p in allPallets)
            {
                var mapped = AutoMapper.Mapper.Map(p, new PalletSync());

                if (p.Dispatch != null)
                {
                    mapped.IsDispatched = p.DispatchTime.HasValue;
                    mapped.DateCompleted = p.Dispatch.DateCompleted;
                }
                mapped.DateUpdated = p.DateUpdated;
                mapped.PalletDispatchInfo = AutoMapper.Mapper.Map<PalletsDispatch, PalletDispatchSync>(p.Dispatch);
                palletSyncs.Add(mapped);
            }

            result.Count = palletSyncs.Count;
            result.TerminalLogId = TerminalServices
                .CreateTerminalLog(reqDate, terminal.TenantId, palletSyncs.Count, terminal.TerminalId,
                    TerminalLogTypeEnum.PalletingSync).TerminalLogId;
            result.Pallets = palletSyncs;
            return Ok(AutoMapper.Mapper.Map(result, new PalletsSyncCollection()));
        }

        public IHttpActionResult GetPalletDispatches(DateTime reqDate, string serialNo)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            reqDate = TerminalServices.GetTerminalSyncDate(reqDate, terminal.TenantId);

            var result = new PalletsDispatchSyncCollection();

            var allPallets = _palletService.GetAllPalletsDispatch(null, reqDate);

            result.PalletDispatchSync = AutoMapper.Mapper.Map<List<PalletsDispatch>, List<PalletDispatchSync>>(allPallets);
            result.Count = allPallets.Count;
            result.TerminalLogId = TerminalServices
                .CreateTerminalLog(reqDate, terminal.TenantId, allPallets.Count, terminal.TerminalId,
                    TerminalLogTypeEnum.PalletingSync).TerminalLogId;

            return Ok(AutoMapper.Mapper.Map(result, new PalletsDispatchSyncCollection()));
        }

        // GET http://ganetest.qsrtime.net/api/sync/pallet-status/{serialNo}
        // GET http://ganetest.qsrtime.net/api/sync/pallet-status/920013c000814
        [HttpGet]
        public IHttpActionResult UpdatePalletStatus(string serialNo, int palletId, int statusId, Guid? palletnumber)
        {
            serialNo = serialNo.Trim().ToLower();

            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var result = _palletService.UpdatePalletStatus(palletId, statusId == 3, palletnumber);
            return Ok(AutoMapper.Mapper.Map(result, new PalletSync()));
        }

        [HttpPost]
        public IHttpActionResult UpdatePalletImages(PalletSync pallet)
        {
            var terminal = TerminalServices.GetTerminalBySerial(pallet.SerialNumber);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(pallet.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            var result = _palletService.UpdatePalletProof(pallet.PalletNumber, pallet.ProofOfLoadingImageBytes);
            return Ok(AutoMapper.Mapper.Map(result, new PalletSync()));
        }

        [HttpGet]
        public IHttpActionResult GetPalletProducts(DateTime reqDate, string serialNo)
        {
            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            reqDate = TerminalServices.GetTerminalSyncDate(reqDate, terminal.TenantId);

            var result = new PalletProductsSyncCollection
            {
                PalletProducts = _palletService.GetAllPalletProductsForSync(reqDate),
                SerialNo = serialNo
            };

            result.Count = result.PalletProducts.Count;
            result.TerminalLogId = TerminalServices.CreateTerminalLog(reqDate, terminal.TenantId, result.Count, terminal.TerminalId, TerminalLogTypeEnum.PalletProductsSync).TerminalLogId;

            return Ok(result);
        }

        [HttpGet]
        public IHttpActionResult GetPalletDispatchMethods(DateTime reqDate, string serialNo)
        {
            var terminal = TerminalServices.GetTerminalBySerial(serialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }
            var result = new PalletDispatchMethodSyncCollection()
            {
                PalletDispatchMethods = _palletService.GetAllSentMethods().Select(m => new PalletDispatchMethodSync()
                { SentMethod = m.Name, SentMethodID = m.SentMethodID }).ToList(),
                SerialNo = serialNo
            };
            result.Count = result.PalletDispatchMethods.Count();
            result.TerminalLogId = TerminalServices
                .CreateTerminalLog(reqDate, terminal.TenantId, result.Count, terminal.TerminalId, TerminalLogTypeEnum.PalletDispatchMethodsSync).TerminalLogId;
            return Ok(result);
        }

        [HttpPost]
        public IHttpActionResult UpdatePalletProducts(PalletProductsSyncCollection palletProductCollection)
        {
            var terminal = TerminalServices.GetTerminalBySerial(palletProductCollection.SerialNo);

            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(palletProductCollection.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            foreach (var item in palletProductCollection.PalletProducts)
            {
                var result = _palletService.AddFulFillmentPalletProduct(AutoMapper.Mapper.Map<PalletProductsSync, PalletProductAddViewModel>(item));
                if (result != null && result.PalletProductID > 0)
                {
                    item.PalletProductID = result.PalletProductID;
                    item.PostedSuccess = true;
                }
            }
            return Ok(palletProductCollection);
        }

        [HttpPost]
        public IHttpActionResult DispatchPallet(PalletSync pallet)
        {
            var terminal = TerminalServices.GetTerminalBySerial(pallet.SerialNumber);
            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(pallet.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            var result = _palletService.DispatchPalletsFromHandheld(pallet, pallet.CreatedBy);
            return Ok(result);
        }

        [HttpPost]
        public IHttpActionResult PostDispatchProgress(PalletDispatchProgress progress)
        {
            var terminal = TerminalServices.GetTerminalBySerial(progress.SerialNo);
            if (terminal == null)
            {
                return Unauthorized();
            }

            var TransactionLog = TerminalServices.CheckTransactionLog(progress.TransactionLogId, terminal.TerminalId);

            if (TransactionLog == true)
            {
                return Conflict();
            }

            var result = _palletService.UpdateDispatchProgress(progress, progress.CreatedBy);

            return Ok(result);
        }
    }
}