﻿using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Ganedata.Core.Services
{
    public class AccountServices : IAccountServices
    {
        private readonly IApplicationContext _currentDbContext;

        public AccountServices(IApplicationContext currentDbContext)
        {
            _currentDbContext = currentDbContext;
        }

        public IEnumerable<Account> GetAllValidAccounts(int tenantId, EnumAccountType customerType = EnumAccountType.All, string searchString = null, DateTime? lastUpdated = null, bool includeIsDeleted = false)
        {
            var accounts = _currentDbContext.Account.Where(a => a.TenantId == tenantId && (includeIsDeleted || a.IsDeleted != true)&& (customerType == EnumAccountType.All ||
            (customerType == EnumAccountType.Customer && (a.AccountTypeCustomer || a.AccountTypeEndUser)) || (customerType == EnumAccountType.Supplier && a.AccountTypeSupplier) || (customerType == EnumAccountType.EndUser && a.AccountTypeEndUser)));
            if (!string.IsNullOrEmpty(searchString))
            {
                accounts = accounts.Where(m => m.CompanyName.Contains(searchString));
            }
            if (lastUpdated.HasValue)
            {
                accounts = accounts.Where(a => (a.DateUpdated ?? a.DateCreated) >= lastUpdated);
            }

            return accounts;
        }


        public IQueryable<Account> GetAllValidAccountsCustom(int tenantId, EnumAccountType customerType = EnumAccountType.All, string searchString = null, DateTime? lastUpdated = null)
        {
            var accounts = _currentDbContext.Account.AsNoTracking().Where(a => a.TenantId == tenantId && a.IsDeleted != true && (customerType == EnumAccountType.All || (customerType == EnumAccountType.Customer && (a.AccountTypeCustomer || a.AccountTypeEndUser)) || (customerType == EnumAccountType.Supplier && a.AccountTypeSupplier) || (customerType == EnumAccountType.EndUser && a.AccountTypeEndUser))
            );
            if (!string.IsNullOrEmpty(searchString))
            {
                accounts = accounts.Where(m => m.CompanyName.Contains(searchString));
            }
            if (lastUpdated.HasValue)
            {
                accounts = accounts.Where(a => (a.DateUpdated ?? a.DateCreated) >= lastUpdated);
            }

            return accounts;
        }

        public IEnumerable<ProductAccountCodes> GetAllValidProductAccountCodes(int productId, int? accountId = null)
        {
            return _currentDbContext.ProductAccountCodes.AsNoTracking().Where(m => m.ProductId == productId && m.IsDeleted != true && (!accountId.HasValue || m.AccountID == accountId)).ToList();
        }

        public IQueryable<Account> GetAllValidAccountbyList(List<int> accountId)
        {
            return _currentDbContext.Account.AsNoTracking().Where(m => accountId.Contains(m.AccountID) && m.IsDeleted !=true);
        }

        public IEnumerable<ProductAccountCodes> GetAllProductAccountCodesByAccount(int accountId)
        {
            return _currentDbContext.ProductAccountCodes.Where(m => m.IsDeleted != true && m.AccountID == accountId).ToList();
        }

        public Account GetAccountsById(int accountId)
        {
            return _currentDbContext.Account.AsNoTracking().FirstOrDefault(x => x.AccountID == accountId && x.IsDeleted !=true);
        }

        public Account GetAccountsByCode(string accountCode, int tenantId)
        {
            return _currentDbContext.Account.FirstOrDefault(m => m.AccountCode.Equals(accountCode, StringComparison.CurrentCultureIgnoreCase) && m.TenantId == tenantId && m.IsDeleted != true);
        }

        public ProductAccountCodes GetProductAccountCodesById(int productAccountCodeId)
        {
            return _currentDbContext.ProductAccountCodes.Find(productAccountCodeId);
        }

        public ProductAccountCodes SaveProductAccount(ProductAccountCodes model, int productId, int tenantId, int userId)
        {
            if (model.ProdAccCodeID == 0)
            {
                if (_currentDbContext.ProductAccountCodes.Count(a => a.AccountID == model.AccountID && a.ProdAccCode == model.ProdAccCode && a.IsDeleted != true & a.TenantId == tenantId) > 0)
                {
                    return null;
                }
                model.UpdateCreatedInfo(userId);
                model.TenantId = tenantId;
                model.ProductId = productId;
                _currentDbContext.ProductAccountCodes.Add(model);
            }
            else
            {
                if (_currentDbContext.ProductAccountCodes
                        .Count(a =>
                            a.AccountID == model.AccountID
                            && a.ProdAccCode == model.ProdAccCode
                            && a.ProdAccCodeID != model.ProdAccCodeID
                            && a.IsDeleted != true &&
                            a.TenantId == tenantId) > 0)
                {
                    return null;
                }
                model.UpdatedBy = userId;
                model.DateUpdated = DateTime.UtcNow;
                _currentDbContext.ProductAccountCodes.Attach(model);
                var entry = _currentDbContext.Entry(model);
                entry.Property(e => e.AccountID).IsModified = true;
                entry.Property(e => e.ProdAccCode).IsModified = true;
            }
            _currentDbContext.SaveChanges();
            return model;
        }

        public void DeleteProductAccount(int productAccountId, int userId)
        {
            var current = _currentDbContext.ProductAccountCodes.First(m => m.ProdAccCodeID == productAccountId);
            current.IsDeleted = true;
            current.UpdatedBy = userId;
            current.DateUpdated = DateTime.UtcNow;
            _currentDbContext.SaveChanges();
        }

        public IEnumerable<AccountContacts> GetAllTopAccountContactsByTenantId(int tenantId)
        {
            return _currentDbContext.AccountContacts.Where(a => a.IsDeleted != true && a.AccountID == _currentDbContext.Account.FirstOrDefault(aa => aa.TenantId == tenantId && aa.IsDeleted != true).AccountID);
        }

        public IEnumerable<AccountContacts> GetAllValidAccountContactsByAccountId(int accountId, int tenantId)
        {
            return _currentDbContext.AccountContacts.Where(a => a.IsDeleted != true && a.AccountID == accountId && a.Account.TenantId == tenantId);
        }

        public IEnumerable<AccountAddresses> GetAllValidAccountAddressesByAccountId(int accountId)
        {
            return _currentDbContext.AccountAddresses.Where(c => c.AccountID == accountId && c.IsDeleted != true);
        }

        public AccountAddresses GetAccountAddressById(int id)
        {
            return _currentDbContext.AccountAddresses.Find(id);
        }
        public IEnumerable<AccountAddresses> GetAccountAddress()
        {
            return _currentDbContext.AccountAddresses.Where(u=>u.IsDeleted != true);
        }

        public AccountAddresses DeleteAccountAddress(int addressId, int currentUserId)
        {
            if (addressId == 0)
            {
                return null;
            }

            var customeraddresses = GetAccountAddressById(addressId);
            if (customeraddresses == null)
            {
                return null;
            }
            customeraddresses.DateUpdated = DateTime.UtcNow;
            customeraddresses.UpdatedBy = currentUserId;
            customeraddresses.IsDeleted = true;

            _currentDbContext.AccountAddresses.Attach(customeraddresses);
            var entry = _currentDbContext.Entry(customeraddresses);
            entry.Property(e => e.IsDeleted).IsModified = true;
            entry.Property(e => e.DateUpdated).IsModified = true;
            entry.Property(e => e.UpdatedBy).IsModified = true;
            var customer = _currentDbContext.Account.Find(customeraddresses.AccountID);
            if (customer == null)
            {
                return null;
            }
            customer.DateUpdated = DateTime.UtcNow;
            customer.UpdatedBy = currentUserId;
            _currentDbContext.Account.Attach(customer);
            var entry1 = _currentDbContext.Entry(customer);
            entry1.Property(e => e.DateUpdated).IsModified = true;
            entry1.Property(e => e.UpdatedBy).IsModified = true;
            _currentDbContext.SaveChanges();
            return customeraddresses;
        }

        public void DeleteAccount(int id, int userId)
        {
            Account customer = _currentDbContext.Account.Find(id);
            customer.DateUpdated = DateTime.UtcNow;
            customer.UpdatedBy = userId;
            customer.IsDeleted = true;

            _currentDbContext.Account.Attach(customer);
            var entry = _currentDbContext.Entry(customer);
            entry.Property(e => e.IsDeleted).IsModified = true;
            entry.Property(e => e.DateUpdated).IsModified = true;
            entry.Property(e => e.UpdatedBy).IsModified = true;

            _currentDbContext.SaveChanges();
        }

        public AccountContacts GetAccountContactById(int id)
        {
            return _currentDbContext.AccountContacts.Find(id);
        }

        public Account SaveAccount(Account model, List<int> accountAddressIds, List<int> accountContactIds, int globalCountryIds, int globalCurrencyIds, int accountStatusIds, int priceGroupId, int ownerUserId, List<AccountAddresses> addresses, List<AccountContacts> contacts, int userId, int tenantId, string stopReason = null)
        {
            var account = _currentDbContext.Account.FirstOrDefault(m => m.AccountID == model.AccountID);

            if (account == null || model.AccountID < 1)
            {
                model.CreatedBy = userId;
                model.TenantId = tenantId;
                model.DateCreated = DateTime.UtcNow;
                model.AccountStatusID = accountStatusIds;
                model.CountryID = globalCountryIds;
                model.CurrencyID = globalCurrencyIds;
                model.OwnerUserId = ownerUserId;
                model.PriceGroupID = priceGroupId;
                _currentDbContext.Account.Add(model);
                _currentDbContext.SaveChanges();

                if (accountAddressIds != null)
                {
                    var addressesToadd = addresses.Where(a => accountAddressIds.Contains(a.AddressID)).ToList();
                    foreach (var item in addressesToadd)
                    {
                        item.AccountID = model.AccountID;
                        item.CreatedBy = userId;
                        item.DateCreated = DateTime.UtcNow;
                        _currentDbContext.AccountAddresses.Add(item);
                    }
                }

                if (accountContactIds != null)
                {
                    var contactsToAdd = contacts.Where(a => accountContactIds.Contains(a.AccountContactId)).ToList();
                    foreach (var item in contactsToAdd)
                    {
                        item.AccountID = model.AccountID;
                        item.CreatedBy = userId;
                        item.DateCreated = DateTime.UtcNow;
                        _currentDbContext.AccountContacts.Add(item);
                    }
                }

                _currentDbContext.SaveChanges();

                var audit = new AccountStatusAudit()
                {
                    CreatedBy = userId,
                    DateCreated = DateTime.UtcNow,
                    UpdatedBy = userId,
                    DateUpdated = DateTime.UtcNow,
                    TenantId = tenantId,
                    AccountId = model.AccountID,
                    LastStatusId = model.AccountStatusID,
                    NewStatusId = model.AccountStatusID,
                    Reason = stopReason ?? "Created"
                };
                _currentDbContext.AccountStatusAudits.Add(audit);
                _currentDbContext.SaveChanges();

            }
            else
            {
                var audit = new AccountStatusAudit()
                {
                    CreatedBy = userId,
                    DateCreated = DateTime.UtcNow,
                    UpdatedBy = userId,
                    DateUpdated = DateTime.UtcNow,
                    TenantId = tenantId,
                    AccountId = model.AccountID,
                    LastStatusId = account.AccountStatusID,
                    NewStatusId = model.AccountStatusID,
                    Reason = (model.AccountStatusID == (int)AccountStatusEnum.OnStop ? stopReason : "")
                };
                account.AccountStatusAudits.Add(audit);

                account.AccountCode = model.AccountCode.Trim();
                account.CompanyName = model.CompanyName.Trim();
                account.DateUpdated = DateTime.UtcNow;
                account.UpdatedBy = userId;
                account.AccountStatusID = model.AccountStatusID;
                account.AccountTypeCustomer = model.AccountTypeCustomer;
                account.AccountTypeEndUser = model.AccountTypeEndUser;
                account.AccountTypeSupplier = model.AccountTypeSupplier;
                account.VATNo = model.VATNo;
                account.RegNo = model.RegNo;
                account.Comments = model.Comments;
                account.AccountEmail = model.AccountEmail;
                account.Telephone = model.Telephone;
                account.Fax = model.Fax;
                account.Mobile = model.Mobile;
                account.website = model.website;
                account.CreditLimit = model.CreditLimit;
                account.CountryID = model.CountryID;
                account.PriceGroupID = model.PriceGroupID;
                account.TaxID = model.TaxID;
                account.CreditTerms = model.CreditTerms;


                if (accountAddressIds == null)
                {
                    var entities = _currentDbContext.AccountAddresses.Where(a => a.AccountID == account.AccountID && a.IsDeleted != true).ToList();
                    foreach (var item in entities)
                    {
                        item.IsDeleted = true;
                        item.DateUpdated = DateTime.UtcNow;
                        item.UpdatedBy = userId;
                        _currentDbContext.Entry(item).State = EntityState.Modified;
                        _currentDbContext.SaveChanges();
                    }
                }

                else
                {
                    var currentAddresses = accountAddressIds.Where(a => a > 0).ToList();
                    var contactsToAdd = accountAddressIds.Where(a => a < 0).ToList();
                    var delete = _currentDbContext
                        .AccountAddresses
                        .Where(a => a.IsDeleted != true && a.AccountID == model.AccountID)
                        .Select(a => a.AddressID)
                        .Except(currentAddresses).ToList();

                    foreach (var item in delete)
                    {
                        var current = _currentDbContext.AccountAddresses.First(a => a.AddressID == item);
                        current.IsDeleted = true;
                        current.UpdatedBy = userId;
                        current.DateUpdated = DateTime.UtcNow;
                    }

                    foreach (var item in contactsToAdd)
                    {
                        var current = addresses.First(a => a.AddressID == item);
                        current.CreatedBy = userId;
                        current.DateCreated = DateTime.UtcNow;
                        current.AccountID = model.AccountID;
                        _currentDbContext.AccountAddresses.Add(current);
                    }
                }

                if (accountContactIds == null)
                {
                    var entities = _currentDbContext.AccountContacts.Where(a => a.AccountID == model.AccountID && a.IsDeleted != true).ToList();
                    foreach (var item in entities)
                    {
                        item.IsDeleted = true;
                        item.DateUpdated = DateTime.UtcNow;
                        item.UpdatedBy = userId;
                    }
                    _currentDbContext.SaveChanges();
                }
                else
                {
                    var currentContacts = accountContactIds.Where(a => a > 0).ToList();

                    var contactsToAdd = accountContactIds.Where(a => a < 0).ToList();

                    var delete = _currentDbContext.AccountContacts.Where(a => a.IsDeleted != true && a.AccountID == model.AccountID).Select(a => a.AccountContactId).ToList();
                    delete.RemoveAll(m => currentContacts.Contains(m));

                    foreach (var item in delete)
                    {
                        var current = _currentDbContext.AccountContacts.First(a => a.AccountContactId == item);
                        current.IsDeleted = true;
                        current.UpdatedBy = userId;
                        current.DateUpdated = DateTime.UtcNow;


                    }
                    foreach (var item in contactsToAdd)
                    {
                        var current = contacts.First(a => a.AccountContactId == item);
                        current.CreatedBy = userId;
                        current.DateCreated = DateTime.UtcNow;
                        current.AccountID = account.AccountID;
                        _currentDbContext.AccountContacts.Add(current);
                    }

                }
                _currentDbContext.Entry(account).State = EntityState.Modified;
                _currentDbContext.SaveChanges();
            }
            return model;
        }

        public AccountContacts SaveAccountContact(AccountContacts model, int userId)
        {
            if (model.AccountContactId == 0)
            {
                model.CreatedBy = userId;
                model.DateCreated = DateTime.UtcNow;
                _currentDbContext.AccountContacts.Add(model);
                _currentDbContext.SaveChanges();
            }
            else
            {
                _currentDbContext.AccountContacts.Attach(model);
                model.UpdatedBy = userId;
                model.DateUpdated = DateTime.UtcNow;
                var entry = _currentDbContext.Entry(model);
                entry.Property(e => e.ContactEmail).IsModified = true;
                entry.Property(e => e.ContactJobTitle).IsModified = true;
                entry.Property(e => e.ContactName).IsModified = true;
                entry.Property(e => e.ConTypeStatment).IsModified = true;
                entry.Property(e => e.ConTypeInvoices).IsModified = true;
                entry.Property(e => e.ConTypeRemittance).IsModified = true;

                entry.Property(e => e.ConTypePurchasing).IsModified = true;
                entry.Property(e => e.ConTypeMarketing).IsModified = true;
                entry.Property(e => e.TenantContactPhone).IsModified = true;
                entry.Property(e => e.TenantContactPin).IsModified = true;
                _currentDbContext.SaveChanges();
            }
            return model;
        }

        public AccountContacts DeleteAccountContact(int contactId, int currentUserId)
        {
            var model = GetAccountContactById(contactId);
            model.UpdatedBy = currentUserId;
            model.DateUpdated = DateTime.UtcNow;
            model.IsDeleted = true;
            _currentDbContext.SaveChanges();
            return model;
        }


        public AccountAddresses SaveAccountAddress(AccountAddresses customeraddresses, int currentUserId)
        {
            if (customeraddresses.AddressID < 1)
            {
                customeraddresses.Name = customeraddresses.Name.Trim();

                customeraddresses.DateCreated = DateTime.UtcNow;
                customeraddresses.DateUpdated = DateTime.UtcNow;
                customeraddresses.CreatedBy = currentUserId;
                customeraddresses.UpdatedBy = currentUserId;
                customeraddresses.IsDeleted = false;

                _currentDbContext.AccountAddresses.Add(customeraddresses);
                var customer = _currentDbContext.Account.First(m => m.AccountID == customeraddresses.AccountID);
                customer.DateUpdated = DateTime.UtcNow;
                customer.UpdatedBy = currentUserId;
                _currentDbContext.Account.Attach(customer);
                var entry1 = _currentDbContext.Entry(customer);
                entry1.Property(e => e.DateUpdated).IsModified = true;
                entry1.Property(e => e.UpdatedBy).IsModified = true;
            }
            else
            {
                if (!String.IsNullOrEmpty(customeraddresses.Name))
                    customeraddresses.Name = customeraddresses.Name.Trim();

                customeraddresses.DateUpdated = DateTime.UtcNow;
                customeraddresses.UpdatedBy = currentUserId;

                _currentDbContext.AccountAddresses.Attach(customeraddresses);
                var entry = _currentDbContext.Entry(customeraddresses);
                entry.Property(e => e.Name).IsModified = true;
                entry.Property(e => e.AddressLine1).IsModified = true;
                entry.Property(e => e.AddressLine2).IsModified = true;
                entry.Property(e => e.AddressLine3).IsModified = true;
                entry.Property(e => e.Telephone).IsModified = true;
                entry.Property(e => e.Town).IsModified = true;
                entry.Property(e => e.County).IsModified = true;
                entry.Property(e => e.PostCode).IsModified = true;
                entry.Property(e => e.CountryID).IsModified = true;
                entry.Property(e => e.AccountID).IsModified = true;
                entry.Property(e => e.DateUpdated).IsModified = true;
                entry.Property(e => e.UpdatedBy).IsModified = true;
                entry.Property(e => e.AddTypeDefault).IsModified = true;
                entry.Property(e => e.AddTypeMarketing).IsModified = true;
                entry.Property(e => e.AddTypeShipping).IsModified = true;
            }
            _currentDbContext.SaveChanges();

            return customeraddresses;
        }

        public AccountTransactionViewModel GetAccountTransactionById(int transactionId)
        {
            var trans = _currentDbContext.AccountTransactions.Find(transactionId);

            if (trans == null)
                return new AccountTransactionViewModel()
                {
                    AccountTransactionTypeId = (int)AccountTransactionTypeEnum.PaidByAccount
                };
            var model = AutoMapper.Mapper.Map<AccountTransaction, AccountTransactionViewModel>(trans);
            model.AccountPaymentMode = trans.AccountPaymentMode?.Description ?? "Invoiced";
            model.AccountTransactionType = trans.AccountTransactionType.Description;
            model.AccountId = trans.AccountId;
            model.AccountName = trans.Account?.AccountNameCode;
            model.AccountTransactionTypeId = trans.AccountTransactionTypeId;
            return model;
        }

        public List<SelectListItem> GetAllAccountPaymentModesSelectList()
        {
            return _currentDbContext.AccountPaymentModes
                .Select(m => new SelectListItem()
                {
                    Value = m.AccountPaymentModeId.ToString(),
                    Text = m.Description
                })
                .ToList();
        }

        public List<SelectListItem> GetAllAccountsSelectList(int tenantId)
        {
            return _currentDbContext.Account.Where(x => x.TenantId == tenantId && x.IsDeleted != true)
                .Select(m => new SelectListItem()
                {
                    Value = m.AccountID.ToString(),
                    Text = m.CompanyName
                })
                .ToList();
        }

        public IQueryable<AccountTransactionViewModel> GetTenantAccountTransactions(int tenantId, int accountId = 0)
        {
            return _currentDbContext.AccountTransactions.AsNoTracking().Where(x => x.TenantId == tenantId && (x.AccountId == accountId || accountId == 0) && x.IsDeleted != true).OrderByDescending(m => m.AccountTransactionId).Select(m => new AccountTransactionViewModel()
            {
                DateCreated = m.DateCreated,
                AccountId = m.AccountId,
                Amount = m.Amount,
                AccountPaymentMode = m.AccountPaymentMode.Description.ToString(),
                AccountTransactionTypeId = m.AccountTransactionTypeId,
                AccountTransactionId = m.AccountTransactionId,
                Notes = m.Notes,
                AccountPaymentModeId = m.AccountPaymentModeId,
                FinalBalance = m.FinalBalance,
                AccountName = m.Account.CompanyName,
                AccountCode = m.Account.AccountCode,
                AccountTransactionType = m.AccountTransactionType.Description
            });
        }

        public string GetLatestAuditComment(int accountId,int TenantId)
        {
            var latestAudit = _currentDbContext.AccountStatusAudits.Where(x => x.AccountId == accountId && x.NewStatusId == (int)AccountStatusEnum.OnStop && x.IsDeleted != true && x.TenantId==TenantId)
                .OrderByDescending(m => m.DateCreated).FirstOrDefault();
            return latestAudit != null ? latestAudit.Reason : "";
        }

        public AccountTransactionFile AddAccountTransactionFile(AccountTransactionFileSync file, int tenantId)
        {
            var accountTransFile = AutoMapper.Mapper.Map(file, new AccountTransactionFile());
            accountTransFile.CreatedBy = file.UserId;
            accountTransFile.DateCreated = file.CreatedDate ?? DateTime.UtcNow;
            accountTransFile.IsDeleted = file.IsDeleted;
            accountTransFile.TenantId = tenantId;
            _currentDbContext.AccountTransactionFiles.Add(accountTransFile);
            _currentDbContext.SaveChanges();
            return accountTransFile;
        }

        public List<AccountStatusAuditViewModel> GetAccountAudits(int accountId)
        {
            var audits =
                (from a in _currentDbContext.AccountStatusAudits
                 join st in _currentDbContext.GlobalAccountStatus on a.LastStatusId equals st.AccountStatusID
                 join usr in _currentDbContext.AuthUsers on a.CreatedBy equals usr.UserId
                 where a.AccountId == accountId
                 select new AccountStatusAuditViewModel()
                 {
                     AccountId = accountId,
                     UpdatedUserId = a.UpdatedBy ?? a.CreatedBy,
                     UserName = usr.UserFirstName,
                     AccountStatusAuditId = a.AccountStatusAuditId,
                     Reason = a.Reason,
                     DateUpdated = (a.DateUpdated ?? a.DateCreated),
                     LastStatus = st.AccountStatus,
                     NewStatus = a.NewStatus.AccountStatus
                 }).OrderByDescending(m => m.DateUpdated);
            return audits.ToList();
        }

        public IEnumerable<AccountContacts> GetAllValidAccountContactsByAccountContactId(int accountId)
        {

            return _currentDbContext.AccountContacts.Where(u => u.AccountID == accountId && u.ConTypePurchasing == true && u.IsDeleted != true);

        }

        public bool UpdateOrderPTenantEmailRecipients(int?[] accountContactId, int orderId, int UserId)
        {
            if (orderId > 0)
            {
                List<OrderPTenantEmailRecipient> accountids = _currentDbContext.OrderPTenantEmailRecipients.Where(a => a.OrderId == orderId && a.IsDeleted != false).ToList();

                if (accountids.Count > 0)
                {
                    accountids.ForEach(u => u.IsDeleted = true);

                }
                var order = _currentDbContext.Order.Find(orderId);
                if (accountContactId != null && accountContactId.Length > 0)
                {

                    foreach (var item in accountContactId)
                    {
                        string email = "";
                        var secondryAddress = _currentDbContext.AccountContacts.Where(u => u.AccountContactId == item).AsNoTracking().FirstOrDefault();
                        if (secondryAddress != null)
                        {
                            email = secondryAddress.ContactEmail;
                        }


                        var recipient = new OrderPTenantEmailRecipient()
                        {
                            OrderId = order.OrderID,
                            EmailAddress = email,
                            AccountContactId = item,
                            UpdatedBy = UserId,
                            DateUpdated = DateTime.UtcNow,



                        };
                        _currentDbContext.OrderPTenantEmailRecipients.Add(recipient);

                    }
                }
                _currentDbContext.SaveChanges();

            }
            return true;

        }

    }
}