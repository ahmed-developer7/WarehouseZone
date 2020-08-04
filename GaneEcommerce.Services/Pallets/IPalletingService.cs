using System;
using System.Collections.Generic;
using System.Linq;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;

namespace Ganedata.Core.Services
{
    public interface IPalletingService
    {
        PalletProduct AddFulFillmentPalletProduct(PalletProductAddViewModel model);
        Pallet CreateNewPallet(int orderProcessId, int userId);
        List<PalletProduct> GetFulFillmentPalletProductsForPallet(int palletId);
        List<PalletProductsSync> GetAllPalletProductsForSync(DateTime? afterDate);
        List<SentMethod> GetAllSentMethods();
        IQueryable<PalletViewModel> GetAllPallets(int? lastXdays = null, PalletStatusEnum? palletStatusEnum = null, int? orderProcessId = null, DateTime? reqDate = null, int? filterByPalletDetail = null,int? dispatchId=null);
        List<PalletsDispatch> GetAllPalletsDispatch(int? dispatchId, DateTime? reqDate,int? orderProcessID= null);
        string GenerateNextPalletNumber(string prefix = null);
        Pallet GetFulfillmentPalletById(int palletId);
        Pallet GetFulfillmentPalletByNumber(string palletNumber);
        decimal GetFulfilledProductQuantity(int orderProcessDetailId);
        PalletDispatchInfoViewModel GetPalletDispatchDetailByPallet(int palletId);
        PalletsDispatch DispatchPallets(PalletDispatchViewModel dispatch, int userId);
        PalletSync DispatchPalletsFromHandheld(PalletSync currentPallet, int userId);
        Pallet UpdatePalletStatus(int palletId, bool isCompleted, Guid? newPalletToken = null, byte[][] palletProofImage = null);
        Pallet UpdatePalletProof(string palletNumber, byte[][] palletProofImage);
        bool DeletePallet(int palletId);
        int DeletePalletProduct(int palletProductId, int userId);
        PalletDispatchProgress UpdateDispatchProgress(PalletDispatchProgress currentDispatch, int userId);

        PalletsDispatch GetPalletsDispatchByDispatchId(int palletDispatchId);

        bool MarkedOrderProcessAsDispatch(int OrderProcessId);
    }
}