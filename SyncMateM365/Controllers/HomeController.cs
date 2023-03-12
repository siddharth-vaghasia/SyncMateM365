using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using System.Diagnostics;
using Microsoft.Identity.Web;
using Microsoft.Identity.Client;

namespace SyncMateM365.Controllers
{
    [Authorize]
    [AuthorizeForScopes(Scopes = new string[] { "api://983aba2d-af8e-48f3-bab4-375186367c5e/access_as_user" , "Calendars.Read", "Calendars.ReadWrite" })]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISubscribeEventService _subscribeEventService;
        private readonly IGetInfoService _getInfoService;

        public HomeController(ILogger<HomeController> logger, ISubscribeEventService subscribeEventService, IGetInfoService getInfoService)
        {
            _logger = logger;
            _subscribeEventService = subscribeEventService;
            _getInfoService = getInfoService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation(string.Format("Starting {0}", "Index"));
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var myValue = HttpContext.Session.GetString("ParentSubscription");
                    var result = await this._subscribeEventService.CallSubscribeEventAPI(myValue);
                    if (string.IsNullOrEmpty(myValue))
                    {
                        HttpContext.Session.SetString("ParentSubscription", result);
                    }
                }
                var isFromAccounts = TempData["FromAccounts"] as string;
                if (!string.IsNullOrEmpty(isFromAccounts))
                {
                    TempData.Remove("FromAccounts");
                    return RedirectToAction("Accounts");
                }
                else
                {
                    return View();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in Index {0}", ex));
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login(string returnUrl = "/")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };
            properties.Parameters.Add("prompt", "select_account");
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> SwitchUser()
        {
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            var isFromAccounts = TempData["FromAccounts"] as string;
            if (string.IsNullOrEmpty(isFromAccounts))
            {
                TempData.Add("FromAccounts", "FromAccounts");
                TempData.Keep("FromAccounts");
            }
            return RedirectToAction("Login", "Home");
        }

        public async Task<IActionResult> Accounts()
        {
            var myValue = HttpContext.Session.GetString("ParentSubscription");
            if (string.IsNullOrEmpty(myValue))
            {
                var result = await this._subscribeEventService.CallSubscribeEventAPI(myValue);
                HttpContext.Session.SetString("ParentSubscription", result);
            }
            var accounts = await this._getInfoService.GetAllUsersInfo();
            return View("Accounts", accounts);
        }

        public async Task<IActionResult> DeleteAccount(string subscriptionid)
        {
            await this._subscribeEventService.DelteSubscribeEventAPI(subscriptionid);
            var accounts = await this._getInfoService.GetAllUsersInfo();
            return View("Accounts", accounts);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}