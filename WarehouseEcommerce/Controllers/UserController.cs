using Ganedata.Core.Entities.Domain;
using Ganedata.Core.Services;
using System.Net;
using System.Web.Mvc;


namespace WarehouseEcommerce.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IActivityServices _activityServices;
        private readonly ITenantsServices _tenantServices;

        public UserController(ICoreOrderService orderService, IPropertyService propertyService, IAccountServices accountServices, ILookupServices lookupServices, IUserService userService, IActivityServices activityServices, ITenantsServices tenantServices)
            : base(orderService, propertyService, accountServices, lookupServices)
        {
            _userService = userService;
            _activityServices = activityServices;
            _tenantServices = tenantServices;
        }



        // GET: /User/Details/5
        public ActionResult Details(int? id)
        {
            if (!caSession.AuthoriseSession()) { return Redirect((string)Session["ErrorUrl"]); }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AuthUser authuser = _userService.GetAuthUserById((int)id);
            if (authuser == null)
            {
                return HttpNotFound();
            }
            return View(authuser);
        }


        //[RequireHttps]
        public ActionResult Login()
        {
            if (caSession.AuthoriseSession())
            {
                return Redirect("~/Home/Index");
            }
            else if (TempData["Error"] != null)
            {
                ViewBag.Error = TempData["Error"].ToString();
            }
            else if (Session["caTenant"] == null)
            {
                return Redirect((string)Session["ErrorUrl"]);
            }

            return View();
        }


        //[RequireHttps]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string UserName, string UserPassword)
        {
            // string redirect to hold the redirect path
            string RedirectController = "user";
            string ReditectAction = "login";

            if (Session["caTenant"] == null)
            {
                TempData["Error"] = "Security token was expired, Enter credentials again";
            }

            else if (ModelState.IsValid)
            {

                caUser user = new caUser();
                bool status;
                status = user.AuthoriseUser(UserName, UserPassword);
                if (status)
                {
                    Session["caUser"] = user;
                    RedirectController = "Home";
                    ReditectAction = "Index";

                    // store login id into session
                    AuthUserLogin Logins = new AuthUserLogin();
                    Session["CurrentUserLoginId"] = _userService.SaveAuthUserLogin(Logins, user.UserId, user.TenantId);

                    if (!caSession.AuthoriseSession())
                    {
                        return Redirect((string)Session["ErrorUrl"]);
                    }
                }
                else
                {
                    ViewBag.Error = "Wrong user information";
                    ReditectAction = "login";
                    return View();
                }
            }
            if (Session["LastUrlFrom"] != null)
            {
                var url = Session["LastUrlFrom"].ToString();
                Session["LastUrlFrom"] = null;
                if (!url.Contains("error"))
                {
                    return Redirect(url);
                }
            }
            return RedirectToAction(ReditectAction, RedirectController);
        }

        public ActionResult logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "User");

        }

    }
}
