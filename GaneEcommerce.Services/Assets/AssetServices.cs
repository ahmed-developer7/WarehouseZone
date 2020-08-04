﻿using Ganedata.Core.Data;
using Ganedata.Core.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ganedata.Core.Entities.Helpers;
using Ganedata.Core.Entities.Enums;
using Ganedata.Core.Models;
using Microsoft.Ajax.Utilities;

namespace Ganedata.Core.Services
{
    public class AssetServices : IAssetServices
    {
        private readonly IApplicationContext _currentDbContext;

        public AssetServices(IApplicationContext currentDbContext)
        {
            _currentDbContext = currentDbContext;
        }
        public IQueryable<Assets> GetAllValidAssets(int TenantId)
        {

            return _currentDbContext.Assets.Where(u => u.TenantId == TenantId && u.IsDeleted != true);

        }
        public IQueryable<AssetLog> GetAllAssetLog(int AssetId)
        {
            return _currentDbContext.AssetLog.Where(u => u.AssetId == AssetId);

        }
        public void SaveAsset(Assets assets) {

            _currentDbContext.Assets.Add(assets);
            _currentDbContext.Entry(assets).State = EntityState.Added;
            _currentDbContext.SaveChanges();
            

        }
        public void RemoveAsset(int AssetId)
        {

            var obj = _currentDbContext.Assets.Find(AssetId);
            obj.IsDeleted = true;
            _currentDbContext.Assets.Attach(obj);
            _currentDbContext.Entry(obj).State = EntityState.Modified;
            _currentDbContext.SaveChanges();


        }
        public void UpdateAsset(Assets assets)
        {
            var obj = _currentDbContext.Assets.Find(assets.AssetId);
            obj.AssetName = assets.AssetName;
            obj.AssetCode = assets.AssetCode;
            obj.AssetTag = assets.AssetTag;
            obj.AssetDescription = assets.AssetDescription;
            obj.DateUpdated = assets.DateUpdated;
            obj.IsActive = assets.IsActive;
            obj.UpdatedBy = assets.UpdatedBy;
            _currentDbContext.Assets.Attach(obj);
            _currentDbContext.Entry(obj).State = EntityState.Modified;
            _currentDbContext.SaveChanges();


        }
        public Assets GetAssetById(int id)
        {
            return _currentDbContext.Assets.FirstOrDefault(u => u.AssetId == id && u.IsDeleted != true);
        }

    }
}