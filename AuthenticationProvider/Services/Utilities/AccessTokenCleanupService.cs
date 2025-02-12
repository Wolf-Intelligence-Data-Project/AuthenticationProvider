using AuthenticationProvider.Interfaces.Services.Tokens;

namespace AuthenticationProvider.Services.Utilities
{
    public class AccessTokenCleanupService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory; // Inject IServiceScopeFactory
        private Timer _timer;

        public AccessTokenCleanupService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Set timer to run every hour (adjust as needed)
            _timer = new Timer(CleanUpExpiredTokens, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private void CleanUpExpiredTokens(object state)
        {
            // Use IServiceScopeFactory to create a scope and resolve IAccessTokenService
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var accessTokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();
                accessTokenService.CleanUpExpiredTokens(); // Call your method
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
