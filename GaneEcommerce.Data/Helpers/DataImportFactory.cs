using CsvHelper;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Domain.ViewModels;
using Ganedata.Core.Entities.Enums;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Ganedata.Core.Data.Helpers
{
    public class DataImportFactory
    {
        private static string DefaultContactName { get; set; } = "Helpdesk";
        private static string DefaultProductBarcode { get; set; } = "00000000";
        public string ImportSupplierAccounts(string importPath, int tenantId, ApplicationContext context = null, int? userId = null, bool withMarketInfo = false)
        {
            if (context == null)
            {
                context = new ApplicationContext();
            }
            var addedSuppliers = 0;
            var updatedSuppliers = 0;
            var currentTenant = context.Tenants.First(m => m.TenantId == tenantId);
            try
            {
                var adminUserId = context.AuthUsers.First(m => m.UserName.Equals("Admin")).UserId;
                var lineNumber = 0;
                string recorednotmatched = "";
                int count = 0;
                List<string> headers = new List<string>();
                List<object> TotalRecored = new List<object>();
                using (var csv = new CsvReader(File.OpenText(importPath)))
                {
                    try
                    {

                    csv.Read();
                    csv.ReadHeader();
                    headers = csv.Context.HeaderRecord.ToList(); headers = headers.ConvertAll(d => d.ToLower());
                    TotalRecored = csv.GetRecords<object>().ToList();

                    }
                    catch (Exception)
                    {

                        return $"File headers mismatch! Please add required headers";
                    }
                }
               
                if (headers.Count >= 10 && headers.Count <= 12)
                {

                }
                else {
                    return $"File headers mismatch! Please add required headers";
                }
            
                if (!headers.Contains("account code") || !headers.Contains("account name") || !headers.Contains("account address 1") || !headers.Contains("account address 2")
                    || !headers.Contains("account address 3") || !headers.Contains("account type") ||
                    !headers.Contains("contact email") || !headers.Contains("phone") || !headers.Contains("fax number") || !headers.Contains("postcode"))
                {
                    return $"File headers mismatch! Please add required headers";
                }
                if (withMarketInfo)
                {
                    if (!headers.Contains("market name") || !headers.Contains("visit frequency"))
                    {

                        return $"File headers mismatch! Please add required headers";

                    }
                }
               
                if (TotalRecored.Count <= 0)
                {
                    return $"The file is Empty";
                }

                using (var fs = File.OpenRead(importPath))
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        lineNumber++;
                        if (line == null || lineNumber == 1)
                        {
                            continue;
                        }
                        var values = GetCsvLineContents(line);
                        if (0 > values.Length || string.IsNullOrEmpty(values[0]))
                        {

                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : account code not found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;
                        }

                        if (1 > values.Length || string.IsNullOrEmpty(values[1]))
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : account name not found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;
                        }
                        if (2 > values.Length || string.IsNullOrEmpty(values[2]))
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : account address 1 not found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;

                        }
                        if (5 > values.Length || string.IsNullOrEmpty(values[5]))
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : account type not found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;

                        }
                        if (withMarketInfo)
                        {
                            if (10 > values.Length || string.IsNullOrEmpty(values[10]))
                            {
                                if (count >= 50) { return recorednotmatched; }
                                recorednotmatched += "Import Failed :market name not found on line :@ " + lineNumber + "<br/> ";
                                count++;
                                continue;

                            }

                        }

                        var countryId = context.Tenants.FirstOrDefault(m => m.TenantId == tenantId).CountryID ?? 0;
                        if (countryId <= 0)
                        {
                            if (count >= 50) { return recorednotmatched; }
                            return "Import Failed : No country found against this user or tenant <br/>";
                        }

                        var CurrencyID = context.Tenants.FirstOrDefault(m => m.TenantId == tenantId)?.CurrencyID;
                        if (CurrencyID == null)
                        {
                            return "Import Failed : No Currency found against this user or tenant <br/>";

                        }
                        var PriceGroupID = context.TenantPriceGroups.FirstOrDefault(m => m.TenantId == tenantId)?.PriceGroupID;

                        if (PriceGroupID == null)
                        {
                            return "Import Failed : No Price group found against this user or tenant <br/>";

                        }







                    }

                }

                if (!string.IsNullOrEmpty(recorednotmatched))
                {
                    return recorednotmatched;
                }
                else
                {


                    using (var fs = File.OpenRead(importPath))
                    using (var reader = new StreamReader(fs))
                    {
                        lineNumber = 0;

                        while (!reader.EndOfStream)
                        {
                            var accountCode = "";
                            var postCode = "";
                            var contactName = "";
                            var AddressLine1 = "";
                            var AddressLine2 = "";
                            var AddressLine3 = "";
                            var marketName = "";

                            var line = reader.ReadLine();
                            lineNumber++;
                            var values = GetCsvLineContents(line);
                            if (lineNumber == 1)
                            {
                                continue;
                            }
                            accountCode = values[0];
                            var existingAccount = context.Account.FirstOrDefault(m => m.AccountCode == accountCode);
                            var addRecord = false;
                            if (existingAccount == null)
                            {
                                addedSuppliers++;
                                addRecord = true;
                                existingAccount = new Account();
                            }
                            else
                            {
                                updatedSuppliers++;
                            }
                            if (0 < values.Length)
                            {
                                existingAccount.AccountCode = values[0];
                            }
                            if (1 < values.Length)
                            {
                                existingAccount.CompanyName = values[1];
                                contactName = values[1];
                            }
                            if (2 < values.Length)
                            {
                                AddressLine1 = values[2];
                            }
                            if (3 < values.Length)
                            {
                                AddressLine2 = values[3];
                                contactName = string.IsNullOrEmpty(values[3]) ? (string.IsNullOrEmpty(values[1]) ? DefaultContactName : values[1]) : values[3];
                            }
                            if (4 < values.Length)
                            {
                                AddressLine3 = values[4];

                            }
                            if (5 < values.Length)
                            {
                                if (values[5] == "0")
                                {
                                    existingAccount.AccountTypeCustomer = true;
                                    existingAccount.AccountTypeEndUser = true;
                                    existingAccount.AccountTypeSupplier = true;
                                }
                                else if (values[5] == "1")
                                {

                                    existingAccount.AccountTypeCustomer = true;
                                }
                                else if (values[5] == "2")
                                {

                                    existingAccount.AccountTypeSupplier = true;
                                }
                                else if (values[5] == "3")
                                {

                                    existingAccount.AccountTypeEndUser = true;
                                }

                            }
                            if (6 <= values.Length)
                            {
                                existingAccount.Fax = values[6];
                            }
                            if (7 < values.Length)
                            {
                                existingAccount.AccountEmail = (string.IsNullOrEmpty(values[7]) || values[7].IndexOf('@') < 1) ? null : values[7];
                            }
                            if (8 < values.Length)
                            {
                                existingAccount.Telephone = values[8];
                            }
                            if (9 < values.Length)
                            {
                                postCode = IsValidUkPostcode(values[9]) ? values[9] : "";
                            }
                            if (10 < values.Length)
                            {
                                marketName = values[10];
                            }

                            existingAccount.AccountStatusID = 1;
                            existingAccount.DateCreated = DateTime.UtcNow;
                            existingAccount.AccountTypeSupplier = true;

                            existingAccount.CreatedBy = userId ?? adminUserId;
                            existingAccount.TaxID = existingAccount.TaxID > 0 ? existingAccount.TaxID : context.GlobalTax.First(m => m.TaxName.Contains("Standard")).TaxID;
                            if (existingAccount.IsDeleted == true)
                            {
                                existingAccount.IsDeleted = true;
                            }

                            existingAccount.TenantId = tenantId;
                            existingAccount.CountryID = context.Tenants.FirstOrDefault(m => m.TenantId == tenantId).CountryID ?? 0;
                            existingAccount.CurrencyID = context.Tenants.FirstOrDefault(m => m.TenantId == tenantId).CurrencyID;
                            existingAccount.PriceGroupID = context.TenantPriceGroups.FirstOrDefault(m => m.TenantId == tenantId).PriceGroupID;
                            existingAccount.OwnerUserId = userId ?? adminUserId;

                            if (string.IsNullOrEmpty(contactName))
                            {
                                contactName = DefaultContactName;
                            }

                            var currentAddress = new AccountAddresses()
                            {
                                Name = contactName,
                                PostCode = postCode,
                                AddressLine1 = AddressLine1,
                                AddressLine2 = AddressLine2,
                                AddressLine3 = AddressLine3,
                                DateCreated = DateTime.UtcNow,
                                IsActive = true,
                                CountryID = context.Tenants.FirstOrDefault(m => m.TenantId == tenantId).CountryID ?? 0

                            };

                            var currentContact = new AccountContacts()
                            {
                                ContactName = contactName,
                                ConTypeInvoices = true,
                                ContactEmail = existingAccount.AccountEmail,
                                TenantContactPhone = existingAccount.Telephone,
                                DateCreated = DateTime.UtcNow,
                                IsActive = true
                            };

                            if (!addRecord)
                            {
                                var existingAddress = existingAccount.AccountAddresses.FirstOrDefault(m => m.Name == currentAddress.Name && m.AddressLine1 == currentAddress.AddressLine1);
                                if (existingAddress == null)
                                {
                                    existingAccount.AccountAddresses.Add(currentAddress);
                                }
                                else
                                {
                                    existingAddress.PostCode = currentAddress.PostCode;
                                    existingAddress.AddressLine1 = currentAddress.AddressLine1;
                                    existingAddress.AddressLine2 = currentAddress.AddressLine2;
                                    existingAddress.AddressLine3 = currentAddress.AddressLine3;
                                    existingAddress.DateUpdated = DateTime.UtcNow;
                                    context.Entry(existingAddress).State = EntityState.Modified;
                                }


                                var existingContact = existingAccount.AccountContacts.FirstOrDefault(m => m.ContactName == currentAddress.Name);
                                if (existingContact == null)
                                {
                                    existingAccount.AccountContacts.Add(currentContact);
                                }
                                else
                                {
                                    existingContact.ContactEmail = currentContact.ContactEmail;
                                    existingContact.TenantContactPhone = currentContact.TenantContactPhone;
                                    existingContact.DateUpdated = DateTime.UtcNow;
                                    context.Entry(existingContact).State = EntityState.Modified;
                                }
                            }
                            else
                            {
                                existingAccount.AccountContacts.Add(currentContact);
                                existingAccount.AccountAddresses.Add(currentAddress);
                            }

                            if (addRecord)
                            {
                                context.Account.Add(existingAccount);
                            }
                            else
                            {
                                context.Entry(existingAccount).State = EntityState.Modified;
                            }
                            if (withMarketInfo)
                            {

                                var market = new Market();
                                if (!context.Markets.Any(s => s.Name.Equals(marketName)))
                                {
                                    market = new Market() { Name = marketName, CreatedBy = userId, TenantId = tenantId, DateCreated = DateTime.UtcNow };
                                    context.Entry(market).State = EntityState.Added;

                                }
                                else
                                {
                                    market = context.Markets.FirstOrDefault(m => m.Name == marketName);
                                }

                                if (!context.MarketCustomers.Any(x => x.AccountId == existingAccount.AccountID))
                                {
                                    var mc = new MarketCustomer() { Customer = existingAccount, Market = market, CreatedBy = userId, TenantId = tenantId, DateCreated = DateTime.UtcNow, VisitFrequency = GetVisitFrequencyEnum(values[4]) };
                                    market.MarketCustomers.Add(mc);
                                }
                                else
                                {
                                    string frequency = "";
                                    if (11 < values.Length)
                                    {
                                        frequency = values[11];
                                    }
                                    var marketCustomer = context.MarketCustomers.FirstOrDefault(x => x.AccountId == existingAccount.AccountID && x.MarketId == market.Id);
                                    if (marketCustomer != null)
                                    {
                                        marketCustomer.VisitFrequency = GetVisitFrequencyEnum(frequency);
                                        marketCustomer.DateUpdated = DateTime.UtcNow;
                                        marketCustomer.UpdatedBy = userId;
                                        context.Entry(marketCustomer).State = EntityState.Modified;
                                    }
                                }
                            }






                        }
                        context.SaveChanges();
                    }


                }


            }
            catch (Exception ex)
            {
                return "Import Failed : " + ex.Message;
            }

            return $"Supplier Account details imported successfully. Added { addedSuppliers }, Updated = { updatedSuppliers }";
        }

        private MarketCustomerVisitFrequency GetVisitFrequencyEnum(string frequency)
        {
            switch (frequency)
            {
                case "Weekly": return MarketCustomerVisitFrequency.Weekly;
                case "4 Weeks": return MarketCustomerVisitFrequency.Monthly;
                case "Daily": return MarketCustomerVisitFrequency.Daily;
                default: return MarketCustomerVisitFrequency.Fortnightly;
            }
        }

        private bool IsValidUkPostcode(string postcodeString)
        {
            if (string.IsNullOrEmpty(postcodeString)) return true;

            var pattern = "^(([gG][iI][rR] {0,}0[aA]{2})|((([a-pr-uwyzA-PR-UWYZ][a-hk-yA-HK-Y]?[0-9][0-9]?)|(([a-pr-uwyzA-PR-UWYZ][0-9][a-hjkstuwA-HJKSTUW])|([a-pr-uwyzA-PR-UWYZ][a-hk-yA-HK-Y][0-9][abehmnprv-yABEHMNPRV-Y]))) {0,}[0-9][abd-hjlnp-uw-zABD-HJLNP-UW-Z]{2}))$";
            var regex = new Regex(pattern);
            return regex.IsMatch(postcodeString);
        }

        public string[] GetCsvLineContents(string csvLine)
        {
            var parser = new TextFieldParser(new StringReader(csvLine)) { HasFieldsEnclosedInQuotes = true };
            parser.SetDelimiters(",");

            var fields = new string[] { };

            while (!parser.EndOfData)
            {
                fields = parser.ReadFields();
            }
            parser.Close();
            return fields;
        }


        public string ImportProducts(string importPath, string groupName, int tenantId, int warehouseId, ApplicationContext context = null, int? userId = null)
        {
            if (context == null)
            {
                context = new ApplicationContext();
            }

            var adminUserId = context.AuthUsers.First(m => m.UserName.Equals("Admin")).UserId;

            var currentLine = "";//To debug issue

            var addedProducts = 0;
            var updatedProducts = 0;

            try
            {
                // department on 
                var department = context.TenantDepartments.FirstOrDefault();
                if (department == null)
                {
                    department = new TenantDepartments()
                    {
                        DepartmentName = department.DepartmentName,
                        DateCreated = DateTime.UtcNow,
                        TenantId = tenantId

                    };
                    context.TenantDepartments.Add(department);
                    context.SaveChanges();
                }

                var group = context.ProductGroups.FirstOrDefault(m => m.ProductGroup.Equals(groupName));
                if (group == null)
                {
                    group = new ProductGroups()
                    {
                        ProductGroup = groupName,
                        CreatedBy = 1,
                        DateCreated = DateTime.UtcNow,
                        IsActive = true,
                        TenentId = tenantId
                    };
                    context.ProductGroups.Add(group);
                    context.SaveChanges();
                }

                var productLotOption = context.ProductLotOptionsCodes.FirstOrDefault();
                if (productLotOption == null)
                {
                    productLotOption = new ProductLotOptionsCodes()
                    {
                        LotOptionCodeId = 1,
                        Description = "Imported Lot option code"
                    };
                    context.ProductLotOptionsCodes.Add(productLotOption);
                    context.SaveChanges();
                }

                var productLotProcess = context.ProductLotProcessTypeCodes.FirstOrDefault();
                if (productLotProcess == null)
                {
                    productLotProcess = new ProductLotProcessTypeCodes()
                    {
                        LotProcessTypeCodeId = 1,
                        Description = "Imported Lot Process Type code"
                    };
                    context.ProductLotProcessTypeCodes.Add(productLotProcess);
                    context.SaveChanges();
                }

                var weightGroup = context.GlobalWeightGroups.FirstOrDefault();
                if (weightGroup == null)
                {
                    weightGroup = new GlobalWeightGroups()
                    {
                        WeightGroupId = 1,
                        Weight = 0,
                        Description = "Imported Weight Group"
                    };
                    context.GlobalWeightGroups.Add(weightGroup);
                    context.SaveChanges();
                }
                var lineNumber = 0;
                string recorednotmatched = "";
                int count = 0;
                List<string> headers = new List<string>();
                List<object> TotalRecored = new List<object>();
                using (var csv = new CsvReader(File.OpenText(importPath)))
                {
                    try
                    {

                    
                    csv.Read();
                    csv.ReadHeader();
                    headers = csv.Context.HeaderRecord.ToList(); headers = headers.ConvertAll(d => d.ToLower());
                    TotalRecored = csv.GetRecords<object>().ToList();
                    }
                    catch (Exception)
                    {

                        return $"File headers mismatch! Please add required headers";
                    }
                }
                if (headers.Count > 8)
                {
                    return $"File headers mismatch! Please add required headers";
                }

                if (!headers.Contains("product code") || !headers.Contains("description") || !headers.Contains("description 2") &&
                    (!headers.Contains("inventory") || !headers.Contains("base unit of measure") || !headers.Contains("unit cost")) || !headers.Contains("unit price")
                     || !headers.Contains("preferred vendor no"))
                {
                    return $"File headers mismatch! Please add required headers";
                }
                if (TotalRecored.Count <= 0)
                {
                    return $"The file is Empty";
                }
                using (var fs = File.OpenRead(importPath))
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        lineNumber++;
                        var values = GetCsvLineContents(line);
                        if (lineNumber == 1 || line == null)
                        {
                            continue;
                        }
                        if (values.Length < 2)
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : No product code and product name found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(values[0]))
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : No product code found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;
                        }
                        if (string.IsNullOrEmpty(values[1]))
                        {
                            if (count >= 50) { return recorednotmatched; }
                            recorednotmatched += "Import Failed : No product name found on line :@ " + lineNumber + "<br/> ";
                            count++;
                            continue;
                        }






                    }

                }
                if (!string.IsNullOrEmpty(recorednotmatched))
                {
                    return recorednotmatched;
                }
                else
                {

                    lineNumber = 0;
                    var isStockLevelSheet = false;
                    using (var fs = File.OpenRead(importPath))
                    using (var reader = new StreamReader(fs))
                    {
                        while (!reader.EndOfStream)
                        {
                            lineNumber++;
                            var line = reader.ReadLine();
                            if ((line == null || lineNumber == 1))
                            {
                                continue;
                            }
                            if (line.Contains("MinStockLevel"))
                            {
                                isStockLevelSheet = true;
                                continue;

                            }
                            currentLine = line;
                            var values = GetCsvLineContents(line);
                            var productCode = values[0];
                            var existingProduct = context.ProductMaster.FirstOrDefault(m => m.SKUCode == productCode);
                            var addRecord = false;
                            if (existingProduct == null)
                            {
                                addRecord = true;
                                addedProducts++;
                                existingProduct = new ProductMaster();
                            }
                            else
                            {
                                updatedProducts++;
                            }

                            existingProduct.SKUCode = productCode;
                            if (2 < values.Length)
                            {
                                existingProduct.Description = isStockLevelSheet ? null : values[2];
                            }
                            if (3 < values.Length)
                            {
                                existingProduct.Name = isStockLevelSheet ? values[3] : values[1];
                            }
                            if (5 < values.Length)
                            {
                                existingProduct.BuyPrice = isStockLevelSheet ? null : string.IsNullOrEmpty(values[5]) ? (decimal?)null : decimal.Parse(values[5]);
                            }

                            existingProduct.DateCreated = DateTime.UtcNow;
                            existingProduct.ProdStartDate = DateTime.UtcNow;
                            existingProduct.IsActive = true;
                            existingProduct.TenantId = tenantId;
                            existingProduct.IsDeleted = false;
                            existingProduct.CreatedBy = userId ?? adminUserId;
                            existingProduct.BarCode = existingProduct.SKUCode;
                            existingProduct.ReorderQty = existingProduct.BuyPrice;
                            existingProduct.UOMId = context.GlobalUOM.FirstOrDefault()?.UOMId ?? 1;
                            existingProduct.DimensionUOMId = context.GlobalUOM.FirstOrDefault()?.UOMId ?? 1;
                            existingProduct.ProductGroup = group;
                            existingProduct.TenantDepartment = department;
                            existingProduct.TaxID = context.GlobalTax.First(m => m.TaxName.Contains("Standard")).TaxID;
                            existingProduct.ProductLotOptionsCodes = productLotOption;
                            existingProduct.ProductLotProcessTypeCodes = productLotProcess;
                            existingProduct.GlobalWeightGroups = weightGroup;


                            Locations currentLocation = null;

                            if (isStockLevelSheet)
                            {
                                var locationCode = "";
                                if (2 < values.Length)
                                {
                                    locationCode = values[2];
                                }
                                var productLocation = context.Locations.FirstOrDefault(m => m.LocationCode.Equals(locationCode));
                                if (productLocation == null)
                                {
                                    productLocation = new Locations()
                                    {
                                        WarehouseId = warehouseId,
                                        AllowPick = true,
                                        AllowPutAway = true,
                                        AllowReplenish = true,
                                        CreatedBy = userId ?? adminUserId,
                                        DateCreated = DateTime.UtcNow,
                                        LocationCode = locationCode,
                                        TenentId = tenantId,
                                        LocationGroupId = context.LocationGroups.First().LocationGroupId,
                                        UOMId = context.GlobalUOM.FirstOrDefault()?.UOMId ?? 1,
                                        DimensionUOMId = context.GlobalUOM.FirstOrDefault()?.UOMId ?? 1,
                                        LocationName = existingProduct.SKUCode,
                                        LocationTypeId = context.LocationTypes.FirstOrDefault()?.LocationTypeId ?? 1,
                                    };

                                }
                                currentLocation = productLocation;
                                existingProduct.ProductLocationsMap.Add(new ProductLocations()
                                {
                                    ProductMaster = existingProduct,
                                    CreatedBy = userId ?? adminUserId,
                                    DateCreated = DateTime.UtcNow,
                                    IsActive = true,
                                    Locations = productLocation,
                                    TenantId = tenantId
                                });
                            }
                            var inventory = "";
                            if (4 < values.Length)
                            {
                                inventory = values[4];
                            }
                            if (!context.InventoryStocks.Any())
                            {
                                existingProduct.InventoryStocks.Add(new InventoryStock()
                                {
                                    WarehouseId = warehouseId,
                                    TenantId = tenantId,
                                    InStock = isStockLevelSheet ? (string.IsNullOrEmpty(inventory) ? 0 : decimal.Parse(inventory)) : 0,
                                    DateCreated = DateTime.UtcNow,
                                    IsActive = true,
                                    CreatedBy = userId ?? adminUserId
                                });
                            }
                            if (!context.InventoryTransactions.Any())
                            {
                                existingProduct.InventoryTransactions.Add(new InventoryTransaction()
                                {
                                    WarehouseId = warehouseId,
                                    TenentId = tenantId,
                                    DateCreated = DateTime.UtcNow,
                                    IsActive = true,
                                    CreatedBy = userId ?? adminUserId,
                                    Location = currentLocation,
                                    Quantity = isStockLevelSheet ? (string.IsNullOrEmpty(inventory) ? 0 : decimal.Parse(inventory)) : 0,
                                    LastQty = context.InventoryStocks.FirstOrDefault(x => x.ProductId == existingProduct.ProductId && x.TenantId == tenantId && x.WarehouseId == warehouseId)?.InStock ?? 0,
                                    IsCurrentLocation = true,
                                    InventoryTransactionTypeId = context.InventoryTransactionTypes.First().InventoryTransactionTypeId
                                });
                            }
                            if (addRecord)
                            {
                                department.Products.Add(existingProduct);
                                context.Entry(department).State = EntityState.Modified;
                            }
                            else
                            {
                                context.Entry(existingProduct).State = EntityState.Modified;
                            }
                        }
                    }

                    context.SaveChanges();

                }
            }
            catch (Exception ex)
            {

                return "Import Failed : " + ex.Message + "Occurred in line :@ " + currentLine;
            }

            return $"Product details imported successfully. Added : { addedProducts }; Updated { updatedProducts }";
        }
        public string ImportProductsPrice(string importPath, int tenantId, int warehouseId, ApplicationContext context = null, int? userId = null, int pricegroupId = 0, int actiondetail = 1)
        {
            if (context == null)
            {
                context = new ApplicationContext();
            }
            var adminUserId = context.AuthUsers.First(m => m.UserName.Equals("Admin")).UserId;
            var lineNumber = 0;
            string recorednotmatched = "";
            int count = 0;
            List<string> headers = new List<string>();
            List<object> TotalRecored = new List<object>();
            using (var csv = new CsvReader(File.OpenText(importPath)))
            {
                try
                {



                    csv.Read();
                    csv.ReadHeader();
                    headers = csv.Context.HeaderRecord.ToList(); headers = headers.ConvertAll(d => d.ToLower());
                    TotalRecored = csv.GetRecords<object>().ToList();
                }
                catch (Exception)
                {
                    return $"File headers mismatch! Please add required headers";
                }
            }
            var group = context.TenantPriceGroups.FirstOrDefault(u => u.PriceGroupID == pricegroupId);
            if (headers.Count > 4)
            {
                return $"File headers mismatch! Please add required headers";
            }
            if (!headers.Contains("sku") || !headers.Contains("special price") || !headers.Contains("start date") || !headers.Contains("end date"))
            {
                return $"File headers mismatch! Please add required headers";
            }
            if (TotalRecored.Count <= 0)
            {
                return $"Empty file, no values to import";
            }
            if (group == null)
            {

                return $"No matching price group found";
            }
            else
            {
                using (var fs = File.OpenRead(importPath))
                using (var reader = new StreamReader(fs))
                {
                    if (reader != null)
                    {
                        while (!reader.EndOfStream)
                        {
                            try
                            {

                                lineNumber++;
                                var line = reader.ReadLine();
                                if (lineNumber == 1)
                                {
                                    continue;
                                }

                                if (line == null)
                                {
                                    if (count >= 50) { return recorednotmatched; }
                                    recorednotmatched += "Import Failed: no record found in line :@ " + lineNumber + "<br/> ";
                                    count++;
                                    continue;
                                }
                                var values = GetCsvLineContents(line);
                                if (values != null || values.Length >= 2)
                                {
                                    var productCode = values[0];
                                    if (string.IsNullOrEmpty(productCode))
                                    {
                                        if (count >= 50) { return recorednotmatched; }
                                        recorednotmatched += "Import failed: product code not found on line:@ " + lineNumber + "<br/> ";
                                        count++;
                                        continue;
                                    }
                                    var existingProduct = context.ProductMaster.FirstOrDefault(m => m.SKUCode.Equals(productCode.Trim(), StringComparison.InvariantCultureIgnoreCase));
                                    if (existingProduct == null)
                                    {
                                        if (count >= 50) { return recorednotmatched; }
                                        recorednotmatched += "Import failed: product not found on line :@ " + lineNumber + "<br/> ";
                                        count++;
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(values[1]))
                                    {
                                        decimal price;
                                        if (!Decimal.TryParse(values[1], out price))
                                        {
                                            recorednotmatched += "Import failed: price not found on line :@ " + lineNumber + "<br/> ";
                                            count++;
                                            continue;
                                        }

                                    }
                                    else
                                    {
                                        recorednotmatched += "Import failed: price not found on line :@ " + lineNumber + "<br/> ";
                                        count++;
                                        continue;
                                    }



                                }
                            }
                            catch (Exception ex)
                            {
                                if (count >= 50) { return recorednotmatched; }
                                recorednotmatched += "Import Failed: " + ex.Message + "occurred on line :@ " + lineNumber + "<br/> ";
                                count++;
                                continue;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(recorednotmatched))
                {
                    return recorednotmatched;
                }
                else
                {
                    DateTimeFormatInfo ukDtfi = new CultureInfo("en-GB", false).DateTimeFormat;
                    var formatStrings = new string[] { @"dd/MM/yyyy", @"d/MM/yyyy", @"d/M/yyyy", @"dd/M/yyyy" };
                    if (actiondetail == 2)
                    {
                        var ProductPriceGroup = context.ProductSpecialPrices.Where(u => u.PriceGroupID == pricegroupId).ToList();
                        ProductPriceGroup.ForEach(u => u.SpecialPrice = 0);
                        context.SaveChanges();
                    }
                    using (var fs = File.OpenRead(importPath))
                    using (var reader = new StreamReader(fs))
                    {
                        while (!reader.EndOfStream)
                        {
                            try
                            {

                                var line = reader.ReadLine();
                                var values = GetCsvLineContents(line);
                                if (values != null || values.Length >= 4)
                                {
                                    var productCode = values[0];
                                    var existingProduct = context.ProductMaster.FirstOrDefault(m => m.SKUCode.Equals(productCode.Trim(), StringComparison.InvariantCultureIgnoreCase));
                                    if (existingProduct != null)
                                    {
                                        decimal specialPrice = 0;
                                        DateTime? startdate = DateTime.MinValue;
                                        DateTime? enddate = DateTime.MinValue;
                                        DateTime sdate = DateTime.UtcNow;
                                        DateTime edate = DateTime.UtcNow;
                                        if (!string.IsNullOrEmpty(values[1]))
                                        {
                                            specialPrice = Convert.ToDecimal(values[1]);

                                        }
                                        if (!string.IsNullOrEmpty(values[2]))
                                        {
                                            if (DateTime.TryParseExact(values[2], formatStrings, ukDtfi, DateTimeStyles.None, out sdate))
                                            {
                                                startdate = sdate;
                                                
                                            }
                                            //else if (DateTime.TryParseExact(values[2], formatStrings, ukDtfi, DateTimeStyles.None, out sdate))
                                            //{
                                            //    startdate = sdate;

                                            //}

                                            else
                                            {
                                                startdate = null;
                                            }
                                        }
                                        else
                                        {
                                            startdate = null;
                                        }
                                        if (!string.IsNullOrEmpty(values[3]))
                                        {
                                            if (DateTime.TryParseExact(values[3], formatStrings, ukDtfi, DateTimeStyles.None, out edate))
                                            {
                                                enddate = edate;
                                            }
                                            //else if (DateTime.TryParseExact(values[3], @"d/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out edate))
                                            //{


                                            //    enddate = edate;

                                            //}
                                            else
                                            {
                                                enddate = null;
                                            }
                                        }
                                        else
                                        {
                                            enddate = null;
                                        }
                                        var ProductSpeialPriceGroup = context.ProductSpecialPrices.FirstOrDefault(u => u.ProductID == existingProduct.ProductId && u.PriceGroupID == pricegroupId);
                                        if (ProductSpeialPriceGroup != null)
                                        {
                                            ProductSpeialPriceGroup.SpecialPrice = specialPrice;
                                            ProductSpeialPriceGroup.StartDate = startdate;
                                            ProductSpeialPriceGroup.EndDate = enddate;
                                            ProductSpeialPriceGroup.DateUpdated = DateTime.UtcNow;
                                            ProductSpeialPriceGroup.TenantId = tenantId;
                                            ProductSpeialPriceGroup.IsDeleted = false;
                                            ProductSpeialPriceGroup.CreatedBy = userId ?? adminUserId;

                                            context.Entry(ProductSpeialPriceGroup).State = EntityState.Modified;


                                        }
                                        else
                                        {
                                            TenantPriceGroupDetail tenantPriceGroupDetail = new TenantPriceGroupDetail();
                                            tenantPriceGroupDetail.ProductID = existingProduct.ProductId;
                                            tenantPriceGroupDetail.PriceGroupID = pricegroupId;
                                            tenantPriceGroupDetail.SpecialPrice = specialPrice;
                                            tenantPriceGroupDetail.StartDate = startdate;
                                            tenantPriceGroupDetail.EndDate = enddate;
                                            tenantPriceGroupDetail.DateCreated = DateTime.UtcNow;
                                            tenantPriceGroupDetail.TenantId = tenantId;
                                            tenantPriceGroupDetail.IsDeleted = false;
                                            tenantPriceGroupDetail.CreatedBy = userId ?? adminUserId;
                                            context.ProductSpecialPrices.Add(tenantPriceGroupDetail);



                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {

                                throw;
                            }


                        }

                        context.SaveChanges();
                    }

                }
            }





            return $"Product details imported successfully. Added : Updated";
        }


        public string ImportJobSubTypes(string importPath, int tenantId, ApplicationContext context = null)
        {
            if (context == null)
            {
                context = new ApplicationContext();
            }
            try
            {
                using (var fs = File.OpenRead(importPath))
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        if (line == null) throw new ArgumentNullException(nameof(line));

                        var values = GetCsvLineContents(line);

                        if (string.IsNullOrEmpty(values[0]) || string.IsNullOrEmpty(values[1]))
                        {
                            continue;
                        }

                        var subTypeName = values[0];


                        var existingJobType = context.JobSubTypes.FirstOrDefault(m => m.Name == subTypeName);

                        if (existingJobType == null)
                        {
                            existingJobType = new JobSubType() { Name = subTypeName, Description = subTypeName, TenantId = tenantId };
                            context.JobSubTypes.Add(existingJobType);
                        }
                        //Just with name, can't do any updates
                        //else
                        //{
                        //    context.Entry(existingJobType).State = EntityState.Modified;
                        //}
                        context.SaveChanges();

                    }
                }

                context.SaveChanges();


            }
            catch (Exception ex)
            {
                return "Import Failed : " + ex.Message;
            }

            return $"Job Account details imported successfully.";
        }



        public bool ImportScanSourceProduct(int tenantId, int userId, ApplicationContext context = null)
        {
            bool status = false;

            try
            {

                string[] manufacturer = ConfigurationManager.AppSettings["ProductImportManufacturerNames"].Split(new string[] { ",", "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var items in manufacturer)
                {
                    for (int i = 1; i <= 1000; i++)
                    {
                        if (context == null)
                        {
                            context = new ApplicationContext();
                        }
                        var productSearchResult = GetScanSourceSearchproduct(i, items);
                        if (productSearchResult.Count > 0)
                        {
                            List<string> itemCodes = new List<string>();
                            foreach (var item in productSearchResult)
                            {
                                //var productDetail = GetScanSourceProductDetial(item.ManufacturerItemNumber);
                                if (item != null && !string.IsNullOrEmpty(item.ScanSourceItemNumber) && !string.IsNullOrEmpty(item.ProductFamilyHeadline))
                                {
                                    var product = context.ProductMaster.AsNoTracking().FirstOrDefault(u => u.SKUCode.Equals(item.ScanSourceItemNumber.Trim(), StringComparison.InvariantCultureIgnoreCase));
                                    var desc = item.Description.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    var productFamilyHeadlines = item.ProductFamilyHeadline.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                                    var productCategories = context.ProductGroups.FirstOrDefault(u => desc.Contains(u.ProductGroup) || productFamilyHeadlines.Contains(u.ProductGroup));

                                    if (product == null)
                                    {
                                        itemCodes.Add(item.ManufacturerItemNumber);
                                        ProductMaster productMaster = new ProductMaster();
                                        productMaster.ManufacturerPartNo = item.ManufacturerItemNumber;
                                        productMaster.SKUCode = item.ScanSourceItemNumber.Trim();
                                        productMaster.TaxID = 1;
                                        productMaster.Name = item.ProductFamily;
                                        //productMaster.CommodityCode = item.CommodityImportCodeNumber;
                                        productMaster.Description = item.Description;
                                        productMaster.UOMId = 1;
                                        productMaster.Serialisable = false;
                                        productMaster.LotOption = false;
                                        productMaster.LotOptionCodeId = 1;
                                        productMaster.LotProcessTypeCodeId = 1;
                                        productMaster.BarCode = item.ScanSourceItemNumber;
                                        //productMaster.PackSize = 0;
                                        productMaster.Height = 0;
                                        productMaster.Width = 0;
                                        productMaster.Depth = 0;
                                        productMaster.Weight = 0;
                                        productMaster.TaxID = 1;
                                        productMaster.WeightGroupId = 1;
                                        productMaster.PercentMargin = 0;
                                        productMaster.Kit = false;
                                        productMaster.IsActive = true;
                                        productMaster.ProdStartDate = DateTime.UtcNow;
                                        productMaster.Discontinued = false;
                                        productMaster.DepartmentId = 1;
                                        productMaster.ProcessByCase = false;
                                        productMaster.ProcessByPallet = false;
                                        productMaster.IsRawMaterial = false;
                                        productMaster.TenantId = tenantId;
                                        productMaster.BestSellerProduct = false;
                                        productMaster.TopProduct = false;
                                        productMaster.SpecialProduct = false;
                                        productMaster.OnSaleProduct = false;
                                        productMaster.ProductGroupId = productCategories?.ProductGroupId;

                                        productMaster.DateCreated = DateTime.UtcNow;
                                        //productMaster.CountryOfOrigion = productDetail.CountryofOrigin;
                                        List<string> path = DownloadImage(item?.ProductFamilyImage, item?.ScanSourceItemNumber, tenantId, userId, item.ItemImage);
                                        productMaster.ProductFiles = new List<ProductFiles>();
                                        foreach (var filepath in path)
                                        {
                                            productMaster.ProductFiles.Add(new ProductFiles
                                            {
                                                ProductId = productMaster.ProductId,
                                                FilePath = filepath,
                                                TenantId = tenantId,
                                                CreatedBy = userId,
                                                DateCreated = DateTime.UtcNow

                                            });
                                        }

                                        context.ProductMaster.Add(productMaster);

                                    }
                                }

                            }

                            context.SaveChanges();

                            // get prices for all items.
                            //TODO: customer number to be added in database against tenant config, static value should be replaced by DB value asap. 
                            var productPrices = GetScanSourceProductPrice("1000003502", itemCodes);
                            if (productPrices != null)
                            {
                                foreach (var price in productPrices)
                                {
                                    var product = context.ProductMaster.FirstOrDefault(x => x.ManufacturerPartNo == price.ItemNumber);
                                    if (product != null)
                                    {
                                        product.BuyPrice = Convert.ToDecimal(price.UnitPrice);
                                        product.SellPrice = Convert.ToDecimal(price.MSRP);
                                        context.Entry(product).State = EntityState.Modified;
                                    }
                                }
                            }

                            context.SaveChanges();
                            context = null;
                            status = true;
                        }
                        else
                        {
                            break;
                        }

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return status;
        }



        public List<ScanSourceSearchproductModel> GetScanSourceSearchproduct(int i, string manufacturer)
        {
            try
            {
                string url = "https://services.scansource.com/apisandbox/product/search?customerNumber=1000003502&Manufacturer=" + manufacturer + "&page=" + i + "&pageSize=99";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", "NzFfEHqpWwDOVL7NR53enDPoNxUtmPI6");
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Method = "GET";
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    List<ScanSourceSearchproductModel> productSearch = JsonConvert.DeserializeObject<List<ScanSourceSearchproductModel>>(result);
                    return productSearch;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
        public ProductDetails GetScanSourceProductDetial(string itemNumber)
        {
            ProductDetails productDetail = new ProductDetails();
            try
            {
                string urlProductDetial = "https://services.scansource.com/apisandbox/product/detail?customerNumber=1000003502&itemNumber=" + itemNumber + "&partNumberType=0";
                var httpWebRequestPd = (HttpWebRequest)WebRequest.Create(urlProductDetial);
                httpWebRequestPd.Headers.Add("Ocp-Apim-Subscription-Key", "NzFfEHqpWwDOVL7NR53enDPoNxUtmPI6");
                httpWebRequestPd.Accept = "application/json";
                httpWebRequestPd.Method = "GET";

                var httpResponsepd = (HttpWebResponse)httpWebRequestPd.GetResponse();
                using (var streamReader = new StreamReader(httpResponsepd.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    productDetail = JsonConvert.DeserializeObject<ProductDetails>(result);
                    return productDetail;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<ScanSourceProductPrice> GetScanSourceProductPrice(string CustomerNumber, List<string> ManufacturerItemNumbers)
        {
            if (ManufacturerItemNumbers == null || ManufacturerItemNumbers.Count < 1)
            {
                return null;
            }

            ScanSourceProductPricePost priceRequest = new ScanSourceProductPricePost();
            priceRequest.CustomerNumber = CustomerNumber;
            priceRequest.Lines = new List<PricingRequestLine>();
            foreach (var item in ManufacturerItemNumbers)
            {
                PricingRequestLine pricingRequestLine = new PricingRequestLine();
                pricingRequestLine.itemNumber = item;
                priceRequest.Lines.Add(pricingRequestLine);
            }

            string urls = "https://services.scansource.com/apisandbox/product/pricing";

            // Uses the System.Net.WebClient and not HttpClient, because .NET 2.0 must be supported.
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.Headers.Add("Ocp-Apim-Subscription-Key", "NzFfEHqpWwDOVL7NR53enDPoNxUtmPI6");
                    string serialisedData = JsonConvert.SerializeObject(priceRequest);
                    var response = client.UploadString(urls, serialisedData);
                    var result = JsonConvert.DeserializeObject<List<ScanSourceProductPrice>>(response);

                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public List<string> DownloadImage(string path, string productId, int tenantId, int userId, string itemImage, bool category = false, List<ProductMedia> productMedia = null)
        {
            string UploadDirectory = @"~/UploadedFiles/Products/";
            int i = 0;
            List<string> values = new List<string>();
            if (!string.IsNullOrEmpty(path))
            {
                if (RemoteFileExists(path))
                {
                    i++;
                    try
                    {
                        if (!Directory.Exists(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString())))
                        {
                            Directory.CreateDirectory(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString()));
                        }

                        string resFileName = HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString() + @"/" + "Product" + i + ".jpg");

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(path, resFileName);
                        values.Add((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"));
                        //SaveProductFile((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"), productId, tenantId, userId);
                    }
                    catch (Exception)
                    {
                    }
                }

            }
            if (!string.IsNullOrEmpty(itemImage))
            {
                if (RemoteFileExists(itemImage))
                {
                    try
                    {
                        i++;
                        if (!Directory.Exists(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString())))
                        {
                            Directory.CreateDirectory(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString()));
                        }

                        string resFileName = HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString() + @"/" + "Product" + i + ".jpg");

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(itemImage, resFileName);
                        values.Add((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"));
                        //SaveProductFile((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"), productId, tenantId, userId);
                    }
                    catch (Exception)
                    {
                    }

                }

            }
            //if (productMedia.Count > 0)
            //{

            //    foreach (var item in productMedia)
            //    {
            //        i++;
            //        if (RemoteFileExists(item.URL))
            //        {
            //            if (!Directory.Exists(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString())))
            //                Directory.CreateDirectory(HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString()));
            //            string resFileName = HttpContext.Current.Server.MapPath(UploadDirectory + productId.ToString() + @"/" + "Product" + i + ".jpg");
            //            try
            //            {
            //                WebClient webClient = new WebClient();
            //                webClient.DownloadFile(item.URL, resFileName);
            //                values.Add((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"));
            //                //SaveProductFile((UploadDirectory.Remove(0, 1) + productId.ToString() + @"/" + "Product" + i + ".jpg"), productId, tenantId, userId);
            //            }
            //            catch (Exception ex)
            //            {


            //            }
            //        }

            //    }

            //}


            return values;

        }


        private bool RemoteFileExists(string url)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }

        public void SaveProductFile(string path, int ProductId, int tenantId, int userId, ApplicationContext _currentDbContext = null)
        {
            try
            {

                if (_currentDbContext == null)
                {
                    _currentDbContext = new ApplicationContext();
                }
                ProductFiles productFiles = new ProductFiles();
                productFiles.FilePath = path;
                productFiles.ProductId = ProductId;
                productFiles.TenantId = tenantId;
                productFiles.CreatedBy = userId;
                productFiles.DateCreated = DateTime.UtcNow;
                _currentDbContext.ProductFiles.Add(productFiles);
                _currentDbContext.SaveChanges();


            }
            catch (Exception)
            {

                throw;
            }

        }




    }
    public class ProductPriceSpecial
    {

        string skucode { get; set; }
        string price { get; set; }

        string startDate { get; set; }
        string endDate { get; set; }



    }
}