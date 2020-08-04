using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;

namespace Ganedata.Core.Services
{
    /// <summary>
    /// TODO:GANE Any product with no sell price or threshold will return 0. Be advised.
    /// </summary>
    public class ProductPriceService : IProductPriceService
    {
        private readonly IProductServices _productService;
        private readonly IAccountServices _accountServices;
        private readonly IApplicationContext _context;

        public ProductPriceService(IProductServices productService, IAccountServices accountServices, IApplicationContext context)
        {
            _productService = productService;
            _accountServices = accountServices;
            _context = context;
        }
        public ProductSaleQueryResponse GetProductSalePriceById(int productId)
        {
            var product = GetProductMasterWithSpecialPrice(productId, 0);
            var thresholdInfo = new ProductSaleQueryResponse() { MinimumThresholdPrice = product.SellPrice ?? 0, SellPrice = product.SellPrice ?? 0 };
            return LoadThresholdInfo(thresholdInfo, product);
        }

        public ProductSaleQueryResponse GetProductPriceThresholdByAccountId(int productId, int? accountId = null)
        {
            if (!accountId.HasValue || accountId == 0)
            {
                return GetProductSalePriceById(productId);
            }

            var product = GetProductMasterWithSpecialPrice(productId, accountId ?? 0);

            var minSellPrice = GetPercentageMarginPrice(product.LandedCost, product.PercentMargin);

            var minThresholdPrice = product.MinThresholdPrice ?? (minSellPrice <= 0 ? product.SellPrice : minSellPrice) ?? 0;

            var account = _accountServices.GetAccountsById(accountId.Value);

            var minSellPriceForAccount = GetProductPriceByAccountId(productId, accountId);

            var finalThresholdPrice = new[] { minThresholdPrice, minSellPriceForAccount, minSellPrice }.Min();

            var thresholdInfo = new ProductSaleQueryResponse() { MinimumThresholdPrice = ConvertBaseRates(accountId ?? 0, finalThresholdPrice), SellPrice = ConvertBaseRates(accountId ?? 0, product.SellPrice ?? 0), LandingCost = ConvertBaseRates(accountId ?? 0, product.LandedCost ?? 0), LandingCostWithMargin = ConvertBaseRates(accountId ?? 0, minSellPrice), PriceGroupID = account.PriceGroupID, PriceGroupPercent = account.TenantPriceGroups.Percent, ProfitMargin = product.PercentMargin };

            return LoadThresholdInfo(thresholdInfo, product);
        }


        public ProductSaleQueryResponse CanTheProductBeSoldAtPriceToAccount(int productId, int accountId, decimal sellingPrice)
        {
            var thresholdInfo = GetProductPriceThresholdByAccountId(productId, accountId);
            var minSellprice = thresholdInfo.MinimumThresholdPrice;

            var product = GetProductMasterWithSpecialPrice(productId, accountId);

            var result = sellingPrice > minSellprice && (product.AllowZeroSale == true || sellingPrice > 0);

            if (!result)
            {
                thresholdInfo.Success = false;
                thresholdInfo.FailureMessage = (product.AllowZeroSale != true && sellingPrice <= 0) ? "Zero price sale not permitted for this product. Contact administrator." : "Selling Price cannot be less than the minimum threshold price.";
            }

            return LoadThresholdInfo(thresholdInfo, product);
        }

        public ProductSaleQueryResponse CanTheProductBeSoldAtPercentageDiscountToAccount(int productId, int accountId, decimal customDiscountPercent)
        {
            var thresholdInfo = GetProductPriceThresholdByAccountId(productId, accountId);

            var minSellprice = thresholdInfo.MinimumThresholdPrice;

            var product = GetProductMasterWithSpecialPrice(productId, accountId);

            var sellingPrice = GetPercentageDiscountedPrice(product.SellPrice, customDiscountPercent);

            var result = sellingPrice > minSellprice && (product.AllowZeroSale == true || sellingPrice > 0);
            if (!result)
            {
                thresholdInfo.Success = false;
                thresholdInfo.FailureMessage = (product.AllowZeroSale != true && sellingPrice <= 0) ? "Zero price sale not permitted for this product. Contact administrator." : "Selling Price cannot be less than the minimum threshold price.";
            }
            return LoadThresholdInfo(thresholdInfo, product);
        }

        public List<ProductPriceHistoryModel> GetProductPriceHistoryForAccount(int productId, int accountid)
        {
            return (from odets in _context.OrderDetail.Where(a => a.ProductId == productId && a.IsDeleted != true && a.Order.AccountID == accountid).OrderByDescending(m => m.DateCreated).ToList()
                    select new ProductPriceHistoryModel()
                    {
                        Id = odets.OrderDetailID,
                        Price = odets.Price,
                        Product = odets.ProductMaster.Name,
                        Timestamp = odets.DateCreated.ToString("dd/MM/yyyy HH:mm"),
                        CurrencySymbol = odets.Order.Account.GlobalCurrency.Symbol,
                        PriceWithDate = odets.DateCreated.ToString("dd/MM/yyyy HH:mm - ") + odets.Order.Account.GlobalCurrency.Symbol + odets.Price.ToString("0.00"),
                        TypeIdentifier = odets.Order.TransactionType.InventoryTransactionTypeId
                    }).Take(5).ToList();
        }

        public decimal GetLastProductPriceForAccount(int productId, int accountid, InventoryTransactionTypeEnum transactionType)
        {
            var priceDetail = _context.OrderDetail.AsNoTracking().OrderByDescending(m => m.DateCreated)
                .FirstOrDefault(a => a.ProductId == productId && a.IsDeleted != true && a.Order.AccountID == accountid &&
                            a.Order.InventoryTransactionTypeId == (int)transactionType);

            if (transactionType == InventoryTransactionTypeEnum.PurchaseOrder)
            {
                return priceDetail != null
                    ? priceDetail.Price
                    : ConvertBaseRates(accountid, _productService.GetProductMasterById(productId).BuyPrice ?? 0);
            }

            if (transactionType == InventoryTransactionTypeEnum.SalesOrder)
            {
                var sellPrice = GetProductMasterWithSpecialPrice(productId, accountid).SellPrice ?? 0.0m;

                if (CanTheProductBeSoldAtPriceToAccount(productId, accountid, sellPrice).Success)
                {
                    return sellPrice;
                }
                return GetProductPriceThresholdByAccountId(productId, accountid).SellPrice;
            }
            return 0.0m;
        }

        public decimal GetLastSoldProductPriceForAccount(int productId, int accountid)
        {
            return GetLastProductPriceForAccount(productId, accountid, InventoryTransactionTypeEnum.SalesOrder);
        }

        public decimal GetLastPurchaseProductPriceForAccount(int productId, int accountId)
        {
            return GetLastProductPriceForAccount(productId, accountId, InventoryTransactionTypeEnum.PurchaseOrder);
        }

        public decimal GetTaxAmountProductPriceForAccount(int productId, int accountId)
        {
            var lastProductPrice = GetLastProductPriceForAccount(productId, accountId, InventoryTransactionTypeEnum.SalesOrder);
            var product = GetProductMasterWithSpecialPrice(productId, accountId);
            if (product.GlobalTax == null || product.GlobalTax.PercentageOfAmount == 0) return 0;

            return (lastProductPrice / 100) * product.GlobalTax.PercentageOfAmount;
        }

        private static decimal GetPercentageDiscountedPrice(decimal? price, decimal customDiscountPercent)
        {
            if (!price.HasValue) return 0;

            if (customDiscountPercent == 0) return price.Value;

            return price.Value - ((price.Value / 100) * customDiscountPercent);
        }
        private static decimal GetPercentageMarginPrice(decimal? price, decimal customMarginPercent)
        {
            if (!price.HasValue) return 0;

            if (customMarginPercent == 0) return price.Value;

            return price.Value + ((price.Value / 100) * customMarginPercent);
        }

        private ProductMaster GetProductMasterWithSpecialPrice(int productId, int accountId)
        {
            var product = _productService.GetProductMasterById(productId);

            var priceGroupId = 0;

            var account = _accountServices.GetAccountsById(accountId);
            if (account != null)
            {
                priceGroupId = account.PriceGroupID;
            }

            var specialPrice = _context.ProductSpecialPrices.FirstOrDefault(m => m.ProductID == productId &&
                                                                                 (priceGroupId == 0 || priceGroupId == m.PriceGroupID) &&
                                                                                 (!m.StartDate.HasValue || m.StartDate < DateTime.UtcNow) && (!m.EndDate.HasValue || m.EndDate > DateTime.UtcNow));
            if (specialPrice != null)
            {
                product.SellPrice = specialPrice.SpecialPrice;
            }
            else if (account != null && account.TenantPriceGroups != null && account.TenantPriceGroups.Percent > 0)
            {
                product.SellPrice = product.SellPrice - ((product.SellPrice * account.TenantPriceGroups.Percent) / 100);
            }

            return product;
        }

        private decimal GetProductPriceByAccountId(int productId, int? accountId = null)
        {
            if (!accountId.HasValue || accountId == 0)
            {
                return GetProductSalePriceById(productId).SellPrice;
            }
            var product = GetProductMasterWithSpecialPrice(productId, accountId ?? 0);

            var account = _accountServices.GetAccountsById(accountId.Value);

            var sellPrice = GetPercentageDiscountedPrice(ConvertBaseRates(accountId ?? 0, product.SellPrice ?? 0), account.TenantPriceGroups.Percent);

            return sellPrice;
        }

        private ProductSaleQueryResponse LoadThresholdInfo(ProductSaleQueryResponse thresholdInfo, ProductMaster product)
        {
            var tenantConfig = _context.TenantConfigs.FirstOrDefault(m => m.TenantId == product.TenantId);
            if (tenantConfig != null)
            {
                thresholdInfo.ShowWarning = tenantConfig.AlertMinimumProductPrice;
                thresholdInfo.CanProceed = !tenantConfig.EnforceMinimumProductPrice;
                thresholdInfo.FailureMessage = tenantConfig.AlertMinimumPriceMessage;
                thresholdInfo.StopMessage = tenantConfig.EnforceMinimumPriceMessage;
            }
            return thresholdInfo;
        }

        private decimal ConvertBaseRates(int accountId, decimal price)
        {
            if (accountId == 0) return price;
            var account = _context.Account.First(a => a.AccountID == accountId);
            var tenant = _context.Tenants.First(m => m.TenantId == account.TenantId);
            if (account.CurrencyID == tenant.CurrencyID)
            {
                return price;
            }

            var currencyRate = _context.TenantCurrencies.FirstOrDefault(m => m.TenantId == tenant.TenantId && m.TenantCurrencyID == account.CurrencyID);
            if (currencyRate == null) return price;

            var rate = _context.TenantCurrenciesExRates.First(m => m.TenantCurrencyID == currencyRate.TenantCurrencyID).Rate;

            return rate * price;
        }


        public ProductSpecialPriceViewModel SaveSpecialProductPrice(int productId, decimal price, int priceGroupId, DateTime? startDate = null, DateTime? endDate = null, int currentTenantId = 0, int userId = 0)
        {
            TenantPriceGroupDetail productPrice = _context.ProductSpecialPrices.FirstOrDefault(x => x.ProductID == productId && x.PriceGroupID == priceGroupId);

            if (productPrice == null)
            {
                productPrice = new TenantPriceGroupDetail()
                {
                    PriceGroupID = priceGroupId,
                    StartDate = startDate,
                    EndDate = endDate,
                    ProductID = productId,
                    SpecialPrice = price,
                    TenantId = currentTenantId
                };
                productPrice.UpdateCreatedInfo(userId);
                _context.ProductSpecialPrices.Add(productPrice);
            }
            else
            {
                productPrice.SpecialPrice = price;
                productPrice.StartDate = startDate;
                productPrice.EndDate = endDate;
                productPrice.ProductID = productId;
                productPrice.PriceGroupID = priceGroupId;
                productPrice.UpdateUpdatedInfo(userId);
                _context.Entry(productPrice).State = EntityState.Modified;
            }
            _context.SaveChanges();
            return AutoMapper.Mapper.Map(productPrice, new ProductSpecialPriceViewModel());
        }

        public ProductSpecialPriceViewModel GetSpecialProductPriceById(int specialProductPriceId)
        {
            var specialPrice = _context.ProductSpecialPrices.FirstOrDefault(m => m.PriceGroupDetailID == specialProductPriceId);

            return specialPrice == null ? null : AutoMapper.Mapper.Map(specialPrice, new ProductSpecialPriceViewModel());
        }

        public bool DeleteSpecialProductPriceById(int specialProductPriceId, int userId)
        {
            var specialPrice = _context.ProductSpecialPrices.Find(specialProductPriceId);
            if (specialPrice != null)
            {
                specialPrice.IsDeleted = true;
                specialPrice.UpdateUpdatedInfo(userId);
                _context.Entry(specialPrice).State = EntityState.Modified;
                _context.SaveChanges();
                return true;
            }
            return false;
        }


        public List<ProductSpecialPriceViewModel> GetAllSpecialProductPrices(int tenantId, int priceGroupId)
        {
            var priceGroup = _context.TenantPriceGroups.Find(priceGroupId);
            var allProducts = _context.ProductMaster.Where(m => m.TenantId == tenantId && m.IsDeleted != true);
            var allSpecialPrices = _context.ProductSpecialPrices.Where(m => m.PriceGroupID == priceGroupId);

            var levels =
                (from p in allProducts
                 join a in allSpecialPrices on p.ProductId equals a.ProductID into tmpGroups
                 from d in tmpGroups.DefaultIfEmpty()
                 select new ProductSpecialPriceViewModel()
                 {
                     ProductName = p.Name,
                     DateCreated = d.DateCreated,
                     PriceGroupID = d.PriceGroupID,
                     StartDate = d.StartDate,
                     EndDate = d.EndDate,
                     PriceGroupName = d.PriceGroup.Name,
                     ProductID = p.ProductId,
                     PriceGroupDetailID = d.PriceGroupDetailID,
                     SpecialPrice = d.SpecialPrice,
                     SkuCode = p.SKUCode
                 });

            return levels.ToList();
        }

        public TenantPriceGroups SavePriceGroup(int priceGroupId, string name, decimal percent, int tenantId, int currentUserId, bool ApplyDiscountOnTotal, bool ApplyDiscountOnSpecialPrice)
        {
            var pg = new TenantPriceGroups();
            if (priceGroupId > 0)
            {
                pg = _context.TenantPriceGroups.FirstOrDefault(m => m.PriceGroupID == priceGroupId);
                pg.Name = name;
                pg.Percent = percent;
                pg.TenantId = tenantId;
                pg.UpdatedBy = currentUserId;
                pg.ApplyDiscountOnSpecialPrice = ApplyDiscountOnSpecialPrice;
                pg.ApplyDiscountOnTotal = ApplyDiscountOnTotal;
                pg.DateUpdated = DateTime.UtcNow;
                _context.Entry(pg).State = EntityState.Modified;
            }
            else
            {
                var priceGroup = _context.TenantPriceGroups.FirstOrDefault(m => m.Name.Equals(name) && m.IsDeleted != true);

                if (priceGroup != null) return null;

                pg = new TenantPriceGroups()
                {
                    CreatedBy = currentUserId,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    Percent = percent,
                    TenantId = tenantId,
                    ApplyDiscountOnTotal = ApplyDiscountOnTotal,
                    ApplyDiscountOnSpecialPrice = ApplyDiscountOnSpecialPrice
                };
                _context.Entry(pg).State = EntityState.Added;
            }
            _context.SaveChanges();
            return pg;
        }
        public bool DeleteProductGroupById(int priceGroupId, int userId)
        {
            var priceGroup = _context.TenantPriceGroups.FirstOrDefault(m => m.PriceGroupID == priceGroupId);
            if (priceGroup != null)
            {
                if (_context.Account.Any(m => m.PriceGroupID == priceGroupId))
                {
                    return false;
                }
                priceGroup.IsDeleted = true;
                priceGroup.UpdateUpdatedInfo(userId);
                _context.Entry(priceGroup).State = EntityState.Modified;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public TenantPriceGroups GetTenantPriceGroupById(int priceGroupId)
        {
            return _context.TenantPriceGroups.FirstOrDefault(x => x.PriceGroupID == priceGroupId);
        }


        public IQueryable<TenantPriceGroups> GetAllTenantPriceGroups(int tenantId, bool includeIsDeleted = false)
        {
            return _context.TenantPriceGroups.Where(x => x.TenantId == tenantId && (includeIsDeleted || x.IsDeleted != true));
        }

        public IQueryable<TenantPriceGroupDetail> GetAllTenantPriceGroupDetails(int tenantId, bool includeIsDeleted = false)
        {
            return _context.ProductSpecialPrices.Where(x => x.TenantId == tenantId && (includeIsDeleted || x.IsDeleted != true));
        }
    }
}