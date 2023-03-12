namespace SyncMateM365.Interface
{
    public interface ITokenService
    {
        public Task<string> GetRefreshToken(string assertion);
        public Task<string> RenewToken(string refreshToken);
    }
}
