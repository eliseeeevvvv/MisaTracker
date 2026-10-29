
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MisaTracker.Core.ORM.DataBase;
using WebPush;

namespace MisaTrackerr.Server.Services
{
    public class PushNotificationService
    {
        private readonly IDbContextFactory<DbConnection> _dbFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            IDbContextFactory<DbConnection> dbFactory,
            IConfiguration configuration,
            ILogger<PushNotificationService> logger)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendToUserAsync(
            int userId,
            string title,
            string body,
            string url = "/")
        {
            var publicKey = _configuration["Vapid:PublicKey"];
            var privateKey = _configuration["Vapid:PrivateKey"];
            var subject = _configuration["Vapid:Subject"];

            if (string.IsNullOrWhiteSpace(publicKey) ||
                string.IsNullOrWhiteSpace(privateKey) ||
                string.IsNullOrWhiteSpace(subject))
            {
                throw new InvalidOperationException(
                    "VAPID keys are not configured.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var subscriptions = await db.PushSubscriptions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var vapidDetails = new VapidDetails(
                subject,
                publicKey,
                privateKey);

            var client = new WebPushClient();

            foreach (var subscription in subscriptions)
            {
                var pushSubscription = new WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth);

                var payload = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        title,
                        body,
                        url
                    });

                try
                {
                    await client.SendNotificationAsync(
                        pushSubscription,
                        payload,
                        vapidDetails);
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Не удалось отправить push пользователю {UserId}.",
                        userId);

                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        db.PushSubscriptions.Remove(subscription);
                    }
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
