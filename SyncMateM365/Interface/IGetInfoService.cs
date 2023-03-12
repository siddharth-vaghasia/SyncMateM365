using Microsoft.Graph;
using SyncMateM365.Models;

namespace SyncMateM365.Interface
{
    public interface IGetInfoService
    {
        public Task<List<IUserEventsCollectionPage>?> GetAllEventAPI();
        public Task<List<UserInfo>?> GetAllUsersInfo();
    }
}
