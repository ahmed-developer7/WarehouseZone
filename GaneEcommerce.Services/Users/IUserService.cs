using System.Collections.Generic;
using Ganedata.Core.Entities.Domain;
using System;

namespace Ganedata.Core.Services
{
    public interface IUserService
    {
        IEnumerable<AuthUser> GetAllAuthUsers(int tenantId);
        List<AuthUserListForGridViewModel> GetAllAuthUsersForGrid(int tenantId);
        AuthUser GetAuthUserById(int userId);
        List<AuthUser> GetAuthUsersByTenantAndDateUpdated(int tenantId, DateTime dateUpdated);
        int SaveAuthUser(AuthUser user, int userId, int tenantId);
        void UpdateAuthUser(AuthUser user, int userId, int tenantId);
        void DeleteAuthUser(AuthUser user, int userId);
        int SaveAuthUserLogin(AuthUserLogin userLogin, int userId, int tenantId);
        int IsUserNameExists(string userName, int tenantId);
        void UpdateAuthUserForPermissions(AuthUser user, int userId, int tenantId);
        int GetResourceIdByUserId(int userId);
        string GetResourceNameByUserId(int? userId);
    }
}