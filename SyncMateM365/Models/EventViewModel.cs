namespace SyncMateM365.Models
{
    public class EventViewModel    {

        public List<EventModel> Events { get; set; }
        public List<UserInfo> UserInfos { get; set; }
    }

    public class EventModel
    {
        public string _id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string backgroundColor { get; set; }
        public string textColor { get; set; }
        public string borderColor { get; set; }

        public string UserPrincipalName { get; set; }
        
        public List<string> attendees { get; set;}
        public string organizer { get; set; }


        public string body { get; set; }

    }
}
