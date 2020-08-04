﻿using AutoMapper;
using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Models;
using Ganedata.Core.Services;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using WMS.Controllers;

namespace Ganedata.Core.Controllers
{
    public class GroupsController : BaseController
    {
        readonly IGroupsServices _groupsServices;

        public GroupsController(ICoreOrderService orderService, IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices, IGroupsServices groupServices) : base(orderService, propertyService, accountServices, lookupServices)
        {
            _groupsServices = groupServices;
        }
        public ActionResult Index(string message = "")
        {
            if (!caSession.AuthoriseSession()) return Redirect((string)Session["ErrorUrl"]);

            // get properties of current tenant
            var tenant = caCurrent.CurrentTenant();

            // get properties of user
            caUser user = caCurrent.CurrentUser();

            try
            {
                if (!String.IsNullOrWhiteSpace(message))
                    ViewBag.Message = message;

                return View("_Listing", Mapper.Map(_groupsServices.GetAllGroups(tenant.TenantId), new List<GroupsViewModel>()));
            }
            catch (Exception e)
            {
                //log exception
                var err = e.Message;

                return View("_EmptyResult");
            }
        }

        public ActionResult GroupsListPartial()
        {
            return PartialView("_GroupsListGridPartial",
                Mapper.Map(_groupsServices.GetAllGroups(CurrentTenantId), new List<GroupsViewModel>()));
        }

        [HttpPost]
        public ActionResult SearchGroupsByName(string groupsName)
        {
            if (!caSession.AuthoriseSession()) return Redirect((string)Session["ErrorUrl"]);

            // get properties of current tenant
            var tenant = caCurrent.CurrentTenant();

            // get properties of user
            caUser user = caCurrent.CurrentUser();

            try
            {
                return PartialView("_Listing", Mapper.Map(_groupsServices.SearchGroupsByName(groupsName, tenant.TenantId), new List<GroupsViewModel>()));
            }
            catch (Exception e)
            {
                //log exception
                var err = e.Message;

                return View("_ErrorResult");
            }
        }
        public ActionResult Add()
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            try
            {
                return View("_CreateEdit", new GroupsViewModel());
            }
            catch (Exception e)
            {
                //log exception
                var err = e.Message;

                return View("_EmptyResult");
            }
        }

        public ActionResult Details(int id, string message = "")
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();

            // get properties of user
            caUser user = caCurrent.CurrentUser();

            try
            {
                if (!String.IsNullOrWhiteSpace(message))
                    ViewBag.Message = message;

                var result = _groupsServices.GetByGroupsId(id);

                return View("_CreateEdit", Mapper.Map(result, new GroupsViewModel()));
            }
            catch (Exception e)
            {
                //log exception
                var err = e.Message;

                return View("_EmptyResult");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Submit(GroupsViewModel groups)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();

            // get properties of user
            caUser user = caCurrent.CurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    if (groups.Id <= 0)
                    {

                        groups.TenantId = tenant.TenantId;
                        groups.DateCreated = DateTime.UtcNow;
                        groups.DateUpdated = DateTime.UtcNow;
                        groups.CreatedBy = user.UserId;
                        groups.UpdatedBy = user.UserId;

                        //insert
                        groups.Id = _groupsServices.Insert(Mapper.Map<Groups>(groups), CurrentUserId);

                        ViewBag.Message = $"Successfully Added on {DateTime.Now}.";
                    }
                    else
                    {
                        //update
                        Groups newGroup = _groupsServices.GetByGroupsId(groups.Id);
                        //groups.TenantId = Tenant.TenantId;
                        newGroup.GroupName = groups.GroupName;
                        newGroup.DateUpdated = DateTime.UtcNow;
                        newGroup.UpdatedBy = user.UserId;
                        _groupsServices.Update(Mapper.Map<Groups>(newGroup), CurrentUserId);

                        ViewBag.Message = $"Successfully Updated on {DateTime.Now}.";
                    }

                    return RedirectToAction("Index", new { message = ViewBag.Message });
                }
                else //ModelState.IsValid is not valid
                {
                    return View("_CreateEdit", Mapper.Map(_groupsServices.GetByGroupsId(groups.Id), new GroupsViewModel()));
                }
            }
            catch (Exception e)
            {
                //log exception
                var err = e.Message;

                return View("_ErrorResult");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult Delete(int Id)
        {
            if (!caSession.AuthoriseSession()) { return Json(new { success = false }); }

            // get properties of tenant
            caTenant tenant = caCurrent.CurrentTenant();

            // get properties of user
            caUser user = caCurrent.CurrentUser();

            try
            {
                _groupsServices.Delete(Mapper.Map<Groups>(new GroupsViewModel { Id = Id }), CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                //catch error here
                var err = e.Message;

            }
            return Json(new { success = false });
        }
    }
}