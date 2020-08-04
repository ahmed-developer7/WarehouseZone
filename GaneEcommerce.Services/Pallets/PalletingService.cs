using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;

namespace Ganedata.Core.Services
{
    public class PalletingService : IPalletingService
    {
        private readonly IApplicationContext _currentDbContext;
        private readonly IMarketServices _marketServices;
        private readonly IUserService _userService;

        public PalletingService(IApplicationContext currentDbContext, IMarketServices marketServices, IUserService userService)
        {
            _currentDbContext = currentDbContext;
            _marketServices = marketServices;
            _userService = userService;
        }
        public PalletProduct AddFulFillmentPalletProduct(PalletProductAddViewModel model)
        {
            var orderProcessDetail = _currentDbContext.OrderProcessDetail.First(m => m.OrderProcessDetailID == model.OrderProcessDetailID);
            var fulfillmentProduct = new PalletProduct()
            {
                OrderID = orderProcessDetail.OrderDetail.OrderID,
                OrderProcessDetailID = orderProcessDetail.OrderProcessDetailID,
                PalletID = model.CurrentPalletID,
                ProductID = model.ProductID,
                Quantity = model.PalletQuantity,
                CreatedBy = model.CreatedBy,
                DateCreated = model.DateCreated
            };

            _currentDbContext.PalletProducts.Add(fulfillmentProduct);
            _currentDbContext.SaveChanges();

            return fulfillmentProduct;
        }

        public List<PalletProduct> GetFulFillmentPalletProductsForPallet(int palletId)
        {
            return _currentDbContext.PalletProducts.Where(m => m.PalletID == palletId && m.IsDeleted != true).ToList();
        }
        public List<PalletProductsSync> GetAllPalletProductsForSync(DateTime? afterDate)
        {
            var palletProducts = _currentDbContext.PalletProducts.AsNoTracking().Where(m => (!afterDate.HasValue || (m.DateUpdated ?? m.DateCreated) >= afterDate) && m.IsDeleted!=true);
            var result = palletProducts.Select(m => new PalletProductsSync()
            {
                AccountID = m.Pallet.RecipientAccountID,
                CurrentPalletID = m.PalletID,
                OrderProcessDetailID = m.OrderProcessDetailID ?? 0,
                OrderID = m.OrderID,
                PalletProductID = m.PalletProductID,
                ProductID = m.ProductID,
                PalletQuantity = m.Quantity
            }).ToList();
            return result;
        }
        public PalletDispatchInfoViewModel GetPalletDispatchDetailByPallet(int palletId)
        {
            var pallet = GetFulfillmentPalletById(palletId);
            var model = new PalletDispatchInfoViewModel();
            if (pallet == null || pallet.DateCompleted == null || pallet.PalletsDispatch == null)
            {
                return null;
            }
            model.PalletID = palletId;
            model.SentMethod = pallet.PalletsDispatch.SentMethod != null ? pallet.PalletsDispatch.SentMethod.Name : "";
            model.TrackingReference = pallet.PalletsDispatch.TrackingReference;
            if (pallet.PalletsDispatch.VehicleDriverResource != null)
            {
                model.DriverName = pallet.PalletsDispatch.VehicleDriverResource.Name;
            }
            model.VehicleNumber = pallet.PalletsDispatch.VehicleIdentifier;
            model.DispatchNotes = pallet.PalletsDispatch.DispatchNotes;
            model.DispatchEvidenceImages = pallet.PalletsDispatch.ProofOfDeliveryImageFilenames;
            model.DispatchDate = pallet.PalletsDispatch.DateCompleted.Value.ToString("dd/MM/yyyy HH:mm");
            return model;
        }

        public List<SentMethod> GetAllSentMethods()
        {
            return _currentDbContext.SentMethods.ToList();
        }

        public Pallet CreateNewPallet(int orderProcessId, int userId)
        {
            var orderProcess = _currentDbContext.OrderProcess.Find(orderProcessId);

            var pallet = new Pallet() { PalletNumber = GenerateNextPalletNumber(), DateCreated = DateTime.UtcNow, CreatedBy = userId, OrderProcessID = orderProcessId, RecipientAccountID = orderProcess.Order.AccountID };
            _currentDbContext.Pallets.Add(pallet);
            _currentDbContext.SaveChanges();
            return pallet;
        }

        public decimal GetFulfilledProductQuantity(int orderProcessDetailId)
        {
            return _currentDbContext.PalletProducts.Where(m => m.OrderProcessDetailID == orderProcessDetailId && m.IsDeleted != true).ToList().Sum(x => x.Quantity);
        }

        public PalletsDispatch DispatchPallets(PalletDispatchViewModel dispatch, int userId)
        {
            if (dispatch.PalletDispatchId > 0)
            {
                var item = _currentDbContext.PalletsDispatches.FirstOrDefault(u => u.PalletsDispatchID == dispatch.PalletDispatchId);
                if (item != null)
                {
                    item.MarketVehicleID = dispatch.MarketVehicleID;
                    item.VehicleDriverResourceID = dispatch.MarketVehicleDriverID;
                    item.ProofOfDeliveryImageFilenames = dispatch.ProofOfDeliveryImageFilenames;
                    item.DispatchNotes = dispatch.DispatchNotes;
                    item.DispatchReference = dispatch.DispatchRefrenceNumber;
                    item.UpdatedBy = userId;
                    item.DateUpdated = DateTime.UtcNow;
                    item.TrackingReference = dispatch.TrackingReference;
                    item.SentMethodID = dispatch.SentMethodID > 0 ? dispatch.SentMethodID : (int?)null;
                    _currentDbContext.Entry(item).State = EntityState.Modified;
                    _currentDbContext.SaveChanges();
                }

                return item;
            }
            else {
                var item = new PalletsDispatch
                {
                    CompletedBy = userId,
                    MarketVehicleID = dispatch.MarketVehicleID,
                    VehicleDriverResourceID = dispatch.MarketVehicleDriverID,
                    ProofOfDeliveryImageFilenames = dispatch.ProofOfDeliveryImageFilenames,
                    DispatchNotes = dispatch.DispatchNotes,
                    DispatchReference = dispatch.DispatchRefrenceNumber,
                    CreatedBy = userId,
                    DateCreated = DateTime.UtcNow,
                    OrderProcessID = int.Parse(!string.IsNullOrEmpty(dispatch.DispatchSelectedPalletIds) ? dispatch.DispatchSelectedPalletIds : "0"),
                    TrackingReference = dispatch.TrackingReference,
                    SentMethodID = dispatch.SentMethodID > 0 ? dispatch.SentMethodID : (int?)null,
                    DateCompleted = DateTime.UtcNow,
                    DispatchStatus = PalletDispatchStatusEnum.Created
                };
                _currentDbContext.PalletsDispatches.Add(item);
                if (!string.IsNullOrEmpty(dispatch.DispatchSelectedPalletIds))
                {
                    int OrderProcessId = int.Parse(dispatch.DispatchSelectedPalletIds);
                    var PalletList = _currentDbContext.Pallets.Where(u => u.OrderProcessID == OrderProcessId).ToList();
                    var orderProcess = _currentDbContext.OrderProcess.FirstOrDefault(u => u.OrderProcessID == OrderProcessId);

                    foreach (var palletId in PalletList)
                    {
                        var pallet = _currentDbContext.Pallets.Find(palletId.PalletID);
                        if (pallet != null && pallet.PalletProducts.Count > 0)
                        {

                            pallet.DateCompleted = DateTime.UtcNow;
                            pallet.DateUpdated = DateTime.UtcNow;
                            pallet.CompletedBy = userId;
                            pallet.PalletsDispatch = item;


                            _currentDbContext.Entry(pallet).State = EntityState.Modified;
                        }
                    }
                    if (orderProcess != null)
                    {
                        orderProcess.OrderProcessStatusId = (int)OrderProcessStatusEnum.Dispatched;
                        orderProcess.DateUpdated = DateTime.UtcNow;
                        orderProcess.UpdatedBy = userId;
                        _currentDbContext.Entry(orderProcess).State = EntityState.Modified;
                    }
                }
                _currentDbContext.SaveChanges();
                return item;
            }
        }

        public Pallet UpdatePalletProof(string palletNumber, byte[][] palletProofImages)
        {
            var pallet = _currentDbContext.Pallets.FirstOrDefault(m => m.PalletNumber == palletNumber);
            if (pallet != null)
            {
                var fileNames = new List<string>();
                foreach (var palletProofImage in palletProofImages)
                {
                    if (palletProofImage != null && (pallet.MobileToken.HasValue || pallet.PalletID > 0))
                    {
                        var filePath = HttpContext.Current.Server.MapPath("~/UploadedFiles/PalletProofs/");
                        var filename = Guid.NewGuid().ToString() + ".png";
                        var bw = new BinaryWriter(File.Open(filePath + filename, FileMode.OpenOrCreate));
                        bw.Write(palletProofImage);
                        bw.Close();
                        fileNames.Add(filename);
                    }
                }
                pallet.ProofOfLoadingImage = string.Join(",", fileNames);
                _currentDbContext.Entry(pallet).State = EntityState.Modified;
                _currentDbContext.SaveChanges();
            }
            return pallet;
        }

        public Pallet UpdatePalletStatus(int palletId, bool isCompleted, Guid? newPalletToken = null, byte[][] palletProofImage = null)
        {
            var pallet = _currentDbContext.Pallets.FirstOrDefault(m => m.MobileToken == newPalletToken);

            if (palletId < 1)
            {
                pallet = pallet ?? new Pallet()
                {
                    PalletNumber = GenerateNextPalletNumber(),
                    CreatedBy = 0,
                    DateCreated = DateTime.UtcNow,
                    MobileToken = newPalletToken,
                    DateUpdated = DateTime.UtcNow,
                    ProofOfLoadingImage = null
                };

                _currentDbContext.Pallets.Add(pallet);
                _currentDbContext.SaveChanges();

                pallet = UpdatePalletProof(pallet.PalletNumber, palletProofImage);

                return pallet;
            }
            return null;
        }

        public Pallet GetFulfillmentPalletById(int palletId)
        {
            return _currentDbContext.Pallets.Find(palletId);
        }

        public Pallet GetFulfillmentPalletByNumber(string palletNumber)
        {
            return _currentDbContext.Pallets.FirstOrDefault(m => m.PalletNumber.Equals(palletNumber));
        }


        public List<PalletsDispatch> GetAllPalletsDispatch(int? dispatchId, DateTime? reqDate, int? orderProcessID = null)
        {
            return _currentDbContext.PalletsDispatches.Where(u => u.DateCompleted.HasValue && (!orderProcessID.HasValue || u.OrderProcessID == orderProcessID) && (!reqDate.HasValue || (u.DateUpdated ?? u.DateCreated) >= reqDate) && (!dispatchId.HasValue || u.PalletsDispatchID == dispatchId)).OrderByDescending(x => x.DateCreated).ToList();
        }

        public IQueryable<PalletViewModel> GetAllPallets(int? lastXdays = null, PalletStatusEnum? palletStatusEnum = null, int? orderProcessId = null, DateTime? reqDate = null, int? filterByPalletDetail = null, int? dispatchId = null)
        {
            var results = _currentDbContext.Pallets.Where(m => (!lastXdays.HasValue || (m.DateCreated > DbFunctions.AddDays(DateTime.UtcNow, -lastXdays.Value))) && (!reqDate.HasValue || (m.DateUpdated ?? m.DateCreated) >= reqDate) && (!dispatchId.HasValue || (m.PalletsDispatchID ?? m.PalletsDispatchID) == dispatchId)
            && m.IsDeleted != true);
            if (orderProcessId.HasValue)
            {
                results = results.Where(m => m.OrderProcessID == orderProcessId);
            }
            //if (palletStatusEnum != null)
            //{
            //    if (palletStatusEnum.Value == PalletStatusEnum.Completed)
            //    {
            //        results = results.Where(m => m.DateCompleted.HasValue);
            //    }
            //    else
            //    {
            //        results = results.Where(m => !m.DateCompleted.HasValue);
            //    }
            //}

            if (filterByPalletDetail.HasValue)
            {
                results = results.Where(u => u.PalletsDispatchID == filterByPalletDetail);
            }

            return results.Select(m => new PalletViewModel()
            {
                PalletNumber = m.PalletNumber,
                RecipientAccountID = m.RecipientAccountID,
                AccountName = m.RecipientAccount.CompanyName,
                AccountCode = m.RecipientAccount.AccountCode,
                DateCreated = m.DateCreated,
                DispatchTime = m.DateCompleted,
                PalletID = m.PalletID,
                Dispatch = m.PalletsDispatch,
                DateUpdated = m.DateUpdated,
                ProductCount = m.PalletProducts.Any() ? true : false,
                OrderProcessID = m.OrderProcessID,
                ScannedOnLoading = m.ScannedOnLoading,
                LoadingScanTime = m.LoadingScanTime,
                ScannedOnDelivered = m.ScannedOnDelivered,
                DeliveredScanTime = m.DeliveredScanTime,
                DispatchStatus = m.PalletsDispatchID

            });
        }

        public string GenerateNextPalletNumber(string prefixText = null)
        {
            var lastPallet = _currentDbContext.Pallets.OrderByDescending(p => p.PalletNumber).FirstOrDefault();

            var prefix = prefixText ?? "PT";
            var lastNumber = 1;
            if (lastPallet != null)
            {
                lastNumber = int.Parse(lastPallet.PalletNumber.Replace(prefix, ""));
            }

            if (lastPallet != null)
            {
                var nextPalletNumber = (lastNumber + 1).ToString("000000000");
                return prefix + nextPalletNumber;
            }
            else
            {
                return prefix + "000000001";
            }
        }

        public PalletSync DispatchPalletsFromHandheld(PalletSync currentDispatch, int userId)
        {
            int? resourceId = _userService.GetResourceIdByUserId(userId);
            if (resourceId == 0) { resourceId = null; }

            var item = new PalletsDispatch
            {
                CompletedBy = userId,
                MarketVehicleID = currentDispatch.PalletDispatchInfo.MarketVehicleID,
                VehicleDriverResourceID = resourceId,
                ProofOfDeliveryImageFilenames = currentDispatch.PalletDispatchInfo.ProofOfDeliveryImageFilenames,
                DispatchNotes = currentDispatch.PalletDispatchInfo.DispatchNotes,
                CreatedBy = userId,
                DateCreated = DateTime.UtcNow,
                TrackingReference = currentDispatch.PalletDispatchInfo.TrackingReference,
                SentMethodID = currentDispatch.PalletDispatchInfo.SentMethodID > 0 ? currentDispatch.PalletDispatchInfo.SentMethodID : (int?)null,
                DateCompleted = currentDispatch.DateCompleted,
                DispatchStatus = PalletDispatchStatusEnum.Created,
                OrderProcessID = currentDispatch.OrderProcessID
            };

            if (item.MarketVehicleID < 1)
            {
                var vehicle = _marketServices.GetMarketVehicleByVehicleNumber(currentDispatch.PalletDispatchInfo.VehicleIdentifier);
                if (vehicle == null)
                {
                    vehicle = _marketServices.SaveMarketVehicle(new MarketVehicle()
                    {
                        CreatedBy = userId,
                        TenantId = currentDispatch.TenantId,
                        DateCreated = currentDispatch.DateCreated,
                        Name = currentDispatch.PalletDispatchInfo.CustomVehicleModel,
                        VehicleIdentifier = currentDispatch.PalletDispatchInfo.VehicleIdentifier
                    }, userId);
                }
                item.MarketVehicleID = vehicle.Id;
            }

            _currentDbContext.PalletsDispatches.Add(item);
            _currentDbContext.SaveChanges();

            foreach (var palletId in currentDispatch.SelectedPallets)
            {
                var pallet = _currentDbContext.Pallets.Find(palletId);

                if (pallet != null)
                {
                    pallet.DateCompleted = DateTime.UtcNow;
                    pallet.DateUpdated = DateTime.UtcNow;
                    pallet.CompletedBy = userId;
                    pallet.PalletsDispatch = item;
                    _currentDbContext.Entry(pallet).State = EntityState.Modified;
                    _currentDbContext.SaveChanges();

                    UpdatePalletProof(pallet.PalletNumber, currentDispatch.ProofOfLoadingImageBytes);
                }

            }

            currentDispatch.IsDispatched = true;
            currentDispatch.CompletedBy = userId;
            currentDispatch.DateCompleted = DateTime.UtcNow;

            //update order process status to dispatched as well
            var orderProcess = _currentDbContext.OrderProcess.Find(currentDispatch.OrderProcessID);
            orderProcess.OrderProcessStatusId = (int)OrderProcessStatusEnum.Dispatched;
            orderProcess.DateUpdated = DateTime.UtcNow;
            orderProcess.UpdatedBy = userId;
            _currentDbContext.Entry(orderProcess).State = EntityState.Modified;
            _currentDbContext.SaveChanges();

            currentDispatch.PalletDispatchInfo = AutoMapper.Mapper.Map<PalletsDispatch, PalletDispatchSync>(item);

            return currentDispatch;
        }

        public bool DeletePallet(int palletId)
        {
            var pallet = _currentDbContext.Pallets.FirstOrDefault(u => u.PalletID == palletId);
            if (pallet != null)
            {
                pallet.IsDeleted = true;

                pallet.DateUpdated = DateTime.UtcNow;
                pallet.UpdatedBy = caCurrent.CurrentUser().UserId;
                if (pallet.PalletProducts != null)
                {
                    foreach (var pp in pallet.PalletProducts)
                    {
                        pp.IsDeleted = true;
                        pp.DateUpdated = DateTime.UtcNow;
                        pp.UpdatedBy = caCurrent.CurrentUser().UserId;
                        _currentDbContext.Entry(pp).State = EntityState.Modified;
                    }
                    _currentDbContext.Entry(pallet).State = EntityState.Modified;
                    _currentDbContext.SaveChanges();
                    return true;
                }

            }

            return false;

        }

        public PalletDispatchProgress UpdateDispatchProgress(PalletDispatchProgress currentDispatch, int userId)
        {
            int? resourceId = _userService.GetResourceIdByUserId(userId);
            if (resourceId == 0) { resourceId = null; }

            var dispatch = _currentDbContext.PalletsDispatches.Find(currentDispatch.DispatchId);

            dispatch.DispatchStatus = currentDispatch.DispatchStatus;
            dispatch.DateUpdated = DateTime.UtcNow;
            dispatch.UpdatedBy = userId;
            dispatch.ReceiverName = currentDispatch.ReceiverName;


            if (currentDispatch.ReceiverSign != null && currentDispatch.DispatchStatus == PalletDispatchStatusEnum.Delivered)
            {
                var filePath = HttpContext.Current.Server.MapPath("~/UploadedFiles/DispatchSign/");
                var filename = Guid.NewGuid().ToString() + ".png";
                var bw = new BinaryWriter(File.Open(filePath + filename, FileMode.OpenOrCreate));
                bw.Write(currentDispatch.ReceiverSign);
                bw.Close();
                dispatch.ReceiverSign = filename;
            }

            _currentDbContext.Entry(dispatch).State = EntityState.Modified;
            _currentDbContext.SaveChanges();

            foreach (var palletId in currentDispatch.ScannedPalletSerials)
            {
                var pallet = _currentDbContext.Pallets.Find(palletId);

                if (pallet != null)
                {
                    pallet.DateCompleted = DateTime.UtcNow;
                    pallet.DateUpdated = DateTime.UtcNow;
                    pallet.CompletedBy = userId;
                    _currentDbContext.Entry(pallet).State = EntityState.Modified;

                    if (currentDispatch.DispatchStatus == PalletDispatchStatusEnum.Loaded)
                    {
                        pallet.ScannedOnLoading = true;
                        pallet.LoadingScanTime = DateTime.UtcNow;
                    }
                    else if (currentDispatch.DispatchStatus == PalletDispatchStatusEnum.Delivered)
                    {
                        pallet.ScannedOnDelivered = true;
                        pallet.DeliveredScanTime = DateTime.UtcNow;
                    }

                    _currentDbContext.SaveChanges();
                }

            }

            //update order process status
            var orderProcess = _currentDbContext.OrderProcess.Find(dispatch.OrderProcessID);
            if (currentDispatch.DispatchStatus == PalletDispatchStatusEnum.Loaded)
            {
                orderProcess.OrderProcessStatusId = 4;
                orderProcess.DateUpdated = DateTime.UtcNow;
            }
            else if (currentDispatch.DispatchStatus == PalletDispatchStatusEnum.Delivered)
            {
                orderProcess.OrderProcessStatusId = 5;
                orderProcess.DateUpdated = DateTime.UtcNow;
            }
            _currentDbContext.Entry(orderProcess).State = EntityState.Modified;
            _currentDbContext.SaveChanges();

            return currentDispatch;
        }

        public int DeletePalletProduct(int palletProductId, int userId)
        {
            var palletproduct = _currentDbContext.PalletProducts.FirstOrDefault(m => m.PalletProductID == palletProductId);
            int palletId = 0;
            if (palletproduct != null)
            {
                try
                {

                    palletproduct.IsDeleted = true;
                    palletproduct.DateUpdated = DateTime.UtcNow;
                    palletproduct.UpdatedBy = userId;
                    _currentDbContext.PalletProducts.Attach(palletproduct);
                    _currentDbContext.Entry(palletproduct).State = EntityState.Modified;
                    _currentDbContext.SaveChanges();
                    palletId = palletproduct.PalletID;

                }
                catch (Exception)
                {
                    return palletId;
                }

            }
            return palletId;
        }

        public bool MarkedOrderProcessAsDispatch(int OrderProcessId)
        {
            var orderprocess = _currentDbContext.OrderProcess.FirstOrDefault(u => u.OrderProcessID == OrderProcessId);
            if (orderprocess != null)
            {
                orderprocess.OrderProcessStatusId = (int)OrderProcessStatusEnum.Dispatched;
                _currentDbContext.Entry(orderprocess).State = EntityState.Modified;
            }
            
            _currentDbContext.SaveChanges();
            return true;
        }


        public PalletsDispatch GetPalletsDispatchByDispatchId(int palletDispatchId)
        {
            return _currentDbContext.PalletsDispatches.FirstOrDefault(u => u.PalletsDispatchID == palletDispatchId);
        }

    }
}