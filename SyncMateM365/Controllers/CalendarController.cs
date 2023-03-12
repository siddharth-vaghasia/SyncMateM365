using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization.Formatters;
using System.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using SyncMateM365.Services;

namespace SyncMateM365.Controllers
{
    [Authorize]
    [AuthorizeForScopes(Scopes = new string[] { "api://983aba2d-af8e-48f3-bab4-375186367c5e/access_as_user", "Calendars.Read", "Calendars.ReadWrite" })]
    public class CalendarController : Controller
    {

        static readonly string[] scopesToAccessDownstreamApi = new string[] { "Calendars.Read", "Calendars.ReadWrite", "offline_access" };

        private readonly UserInfoService _userInfoService;
        private readonly UserMappingService _userMappingService;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ITokenService _tokenService;
        private readonly IEventService _eventService;
        private readonly IGetInfoService _getinfoService;
        private readonly ISubscribeEventService _subscribeEventService;
        public CalendarController(UserInfoService userInfoService,
            UserMappingService userMappingService,
            ITokenAcquisition tokenAcquisition,
            GraphServiceClient graphServiceClient,
            ITokenService tokenHelper,
            IEventService eventService,
            IGetInfoService getInfoService,
            ISubscribeEventService subscribeEventService)
        {
            _userInfoService = userInfoService;
            _userMappingService = userMappingService;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
            _tokenService = tokenHelper;
            _eventService = eventService;
            _getinfoService = getInfoService;
            _subscribeEventService = subscribeEventService;
        }

        public async Task<IActionResult> Index()
        {
            var myValue = HttpContext.Session.GetString("ParentSubscription");
            if (string.IsNullOrEmpty(myValue))
            {
                var subresult = await this._subscribeEventService.CallSubscribeEventAPI(myValue);
                HttpContext.Session.SetString("ParentSubscription", subresult);
            }

            //get the on behalf of token
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes: scopesToAccessDownstreamApi);
            List<IUserEventsCollectionPage> allevents = new List<IUserEventsCollectionPage>();
            EventViewModel eventViewModel = new EventViewModel();
            eventViewModel.Events = new List<Models.EventModel>();
            List<UserInfo> alluserInfo = new List<UserInfo>();
            string[] colors = new string[] { "#3655b1", "#4a6058", "#05bd4c", "#f4d34a", "#ae7119","#ded800", "#785209", "#468f3d", "#e8db49", "#93c963", "#377858", "#e283f1", "#021b4e" };
            int colorIndex = 0;
            var users = new List<UserInfo>();
            //check if the subscription already present

            var userInfopresent = await _userInfoService.GetByUserId(result.Account.HomeAccountId.ToString());
            if (userInfopresent != null)
            {
                var userMappingValue = await _userMappingService.GetAsync(userInfopresent.SubscriptionId);
                var events = await this._graphServiceClient.Me.Events.Request().GetAsync();


                allevents.Add(events);
                if (events != null && events.Count > 0)
                {

                    //  var emailAddress = newevent[0].Attendees.Select(o => o.EmailAddress.Address).ToList();
                    eventViewModel.Events.AddRange(events.Select(event1 => new Models.EventModel()
                    {
                        title = event1.Subject,
                        start = string.Format("{0}Z", event1?.Start?.DateTime.Substring(0, event1.Start.DateTime.Length - 4)),
                        end = string.Format("{0}Z", event1?.End?.DateTime.Substring(0, event1.End.DateTime.Length - 4)),
                        attendees = event1.Attendees.Select(o => o.EmailAddress.Address).ToList(),
                        UserPrincipalName = userInfopresent.UserPrincipalName,
                        backgroundColor = colors[colorIndex],
                        _id = userInfopresent.UserPrincipalName,
                        body = event1.OnlineMeeting != null ? event1.OnlineMeeting.JoinUrl : "Not an online meeting",
                        organizer = event1.Organizer.EmailAddress.Address
                    }));
                }
             
                alluserInfo.Add(userInfopresent);
                users.Add(new UserInfo() { UserPrincipalName = userInfopresent.UserPrincipalName, BackgroundColor = colors[colorIndex] });
                if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id))
                {
                    foreach (var item in userMappingValue.Mappings)
                    {
                        if (item != userInfopresent.SubscriptionId)
                        {
                            var userInfoValue = await _userInfoService.GetBySubscription(item);
                            if (userInfoValue != null)
                            {
                                alluserInfo.Add(userInfoValue);
                                colorIndex++;
                                if (colorIndex > 13)
                                {
                                    colorIndex = 0;
                                }
                                users.Add(new UserInfo() { UserPrincipalName = userInfoValue.UserPrincipalName, BackgroundColor = colors[colorIndex] });
                            }
                            if (userInfoValue != null && userInfoValue.RefreshToken != null)
                            {
                                var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                                var otherEvents = await this._eventService.Get(userToken);
                                allevents.Add(otherEvents);
                                if (otherEvents != null && otherEvents.Count > 0)
                                {
                                    //  var emailAddress = newevent[0].Attendees.Select(o => o.EmailAddress.Address).ToList();
                                    eventViewModel.Events.AddRange((otherEvents.Select(event1 => new Models.EventModel()
                                    {
                                        title = event1.Subject,
                                        start = string.Format("{0}Z", event1?.Start?.DateTime.Substring(0, event1.Start.DateTime.Length - 4)),
                                        end = string.Format("{0}Z", event1?.End?.DateTime.Substring(0, event1.End.DateTime.Length - 4)),
                                        attendees = event1.Attendees.Select(o => o.EmailAddress.Address).ToList(),
                                        UserPrincipalName = userInfoValue.UserPrincipalName,
                                        backgroundColor = colors[colorIndex],
                                        _id = userInfoValue.UserPrincipalName,
                                        body = event1.OnlineMeeting != null ? event1.OnlineMeeting.JoinUrl : "Not an online meeting",
                                        organizer = event1.Organizer.EmailAddress.Address
                                    }))); ;
                                }

                            }
                        }
                    }
                }
            }

          
           
            
            /*var rnd = new Random();
            for (int i = 0; i < eventViewModel?.Events?.Count; i++)
            {
                Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                eventViewModel.Events[i]._id = eventViewModel.Events[i].attendees.Where(k => alluserInfo.Exists(g => g.UserPrincipalName == k)).Select(p => p).FirstOrDefault();

                if (eventViewModel.Events.Exists(o => o._id == eventViewModel.Events[i]._id && !string.IsNullOrWhiteSpace(o.backgroundColor)))
                {
                    
                  //  eventViewModel.Events[i].backgroundColor = eventViewModel.Events.FirstOrDefault(p => p._id == eventViewModel.Events[i]._id && !string.IsNullOrWhiteSpace(p.backgroundColor)).backgroundColor;
                    //eventViewModel.Events[i].textColor = eventViewModel.Events[i].borderColor = eventViewModel.Events[i].backgroundColor;
                }
                else
                {
                //    eventViewModel.Events[i].backgroundColor = "#" + randomColor.Name;
                //    eventViewModel.Events[i].textColor = eventViewModel.Events[i].backgroundColor;
                  *//*  if (colorIndex < colors.Length)
                    {
                        colorIndex++;
                    }*//*
                }
                if (users.Count(c => c.UserPrincipalName == eventViewModel.Events[i].UserPrincipalName) == 0)
                {
                    eventViewModel.Events[i].backgroundColor = "#" + randomColor.Name;
                    eventViewModel.Events[i].textColor = eventViewModel.Events[i].backgroundColor;
                    users.Add(new UserInfo() { UserPrincipalName = eventViewModel.Events[i].UserPrincipalName, BackgroundColor = eventViewModel.Events[i].backgroundColor });
                }

            }*/

            
            eventViewModel.UserInfos = users;
          //  eventViewModel.UserInfos.Add(new UserInfo() { BackgroundColor = "blue", UserPrincipalName = "test1@gmail.com" });*/
            return View(eventViewModel);
        }



    }


}
