﻿using System.Collections.Generic;
using System.Linq;
using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using System;
using System.Data.Entity;
using Ganedata.Core.Entities.Helpers;

namespace Ganedata.Core.Services
{
    public class UserService : IUserService
    {

        private readonly IApplicationContext _currentDbContext;

        public UserService(IApplicationContext currentDbContext)
        {
            _currentDbContext = currentDbContext;
        }

        public IEnumerable<AuthUser> GetAllAuthUsers(int tenantId)
        {
            return _currentDbContext.AuthUsers.Where(m => m.TenantId == tenantId && m.IsDeleted != true).OrderBy(m => m.UserName);
        }

        public List<AuthUserListForGridViewModel> GetAllAuthUsersForGrid(int tenantId)
        {
            return (from p in _currentDbContext.AuthUsers
                    where ((p.TenantId == tenantId) && (p.IsDeleted != true) && (p.SuperUser != true))
                    select new AuthUserListForGridViewModel
                    {
                        UserId = p.UserId,
                        UserName = p.UserName,
                        FirstName = p.UserFirstName,
                        LastName = p.UserLastName,
                        Email = p.UserEmail,
                        DateUpdated = p.DateUpdated,
                        IsActive = p.IsActive
                    }).ToList();
        }

        public AuthUser GetAuthUserById(int userId)
        {
            return _currentDbContext.AuthUsers.Find(userId);
        }
        public AuthUser GetAuthUserByName(string userName)
        {
            return _currentDbContext.AuthUsers.FirstOrDefault(m => m.UserName == userName && m.IsDeleted != true);
        }

        public List<AuthUser> GetAuthUsersByTenantAndDateUpdated(int tenantId, DateTime dateUpdated)
        {
            return _currentDbContext.AuthUsers.Where(e => e.TenantId == tenantId && e.SuperUser != true && (!e.DateUpdated.HasValue || e.DateUpdated > dateUpdated)).ToList();
        }

        public int SaveAuthUser(AuthUser user, int userId, int tenantId)
        {
            user.DateCreated = DateTime.UtcNow;
            user.DateUpdated = DateTime.UtcNow;
            user.CreatedBy = userId;
            user.UpdatedBy = userId;
            user.TenantId = tenantId;

            _currentDbContext.Entry(user).State = EntityState.Added;
            _currentDbContext.SaveChanges();
            return user.UserId;
        }

        public void UpdateAuthUser(AuthUser user, int userId, int tenantId)
        {
            user.UpdateUpdatedInfo(userId);
            _currentDbContext.AuthUsers.Attach(user);
            var entry = _currentDbContext.Entry(user);
            entry.Property("UserName").IsModified = true;
            entry.Property("UserFirstName").IsModified = true;
            entry.Property(e => e.UserLastName).IsModified = true;
            entry.Property(e => e.UserEmail).IsModified = true;
            entry.Property(e => e.IsActive).IsModified = true;
            entry.Property(e => e.DateUpdated).IsModified = true;
            entry.Property(e => e.UpdatedBy).IsModified = true;
            entry.Property(e => e.IsActive).IsModified = true;

            //dont change password if password field is blank/null
            if (user.UserPassword != null)
            {
                // change user password into MD5 hash value
                user.UserPassword = GaneStaticAppExtensions.GetMd5(user.UserPassword);
                entry.Property(e => e.UserPassword).IsModified = true;
            }
            _currentDbContext.SaveChanges();
        }

        public void UpdateAuthUserForPermissions(AuthUser user, int userId, int tenantId)
        {
            user.UpdateUpdatedInfo(userId);
            _currentDbContext.AuthUsers.Attach(user);
            var entry = _currentDbContext.Entry(user);
            entry.Property(e => e.DateUpdated).IsModified = true;
            entry.Property(e => e.UpdatedBy).IsModified = true;
            _currentDbContext.SaveChanges();
        }

        public void DeleteAuthUser(AuthUser user, int userId)
        {
            user.IsDeleted = true;
            user.UpdatedBy = userId;
            user.DateUpdated = DateTime.UtcNow;
            _currentDbContext.Entry(user).State = EntityState.Modified;
            _currentDbContext.SaveChanges();
        }

        public int SaveAuthUserLogin(AuthUserLogin userLogin, int userId, int tenantId)
        {
            userLogin.UserId = userId;
            userLogin.TenantId = tenantId;
            userLogin.DateLoggedIn = DateTime.UtcNow;

            _currentDbContext.Entry(userLogin).State = EntityState.Added;
            _currentDbContext.SaveChanges();
            return userLogin.UserLoginId;
        }

        public int IsUserNameExists(string userName, int tenantId)
        {
            return (from p in _currentDbContext.AuthUsers
                    where (p.UserName == userName && p.TenantId == tenantId && p.IsDeleted != true)
                    select p).Count();
        }

        public int GetResourceIdByUserId(int userId)
        {
            var resource = _currentDbContext.Resources.FirstOrDefault(m => m.AuthUserId == userId && m.IsDeleted != true);
            return resource?.ResourceId ?? 0;
        }

        public string GetResourceNameByUserId(int? userId = 0)
        {
            string name = "";

            if (userId > 0)
            {
                var resource = _currentDbContext.Resources.FirstOrDefault(m => m.AuthUserId == userId && m.IsDeleted != true);
                name = resource?.Name ?? "";
            }

            return name;
        }
    }
}