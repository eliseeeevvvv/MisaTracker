
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.DataBase;
using MisaTracker.Core.ORM.Models;

namespace MisaTrackerr.Server.Controllers
{
    [ApiController]
    [Route("api/push")]
    [Authorize]
    public class PushController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _dbFactory;

        public PushController(IDbContextFactory<DbConnection> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public class PushSubscriptionRequest
        {
            public string Endpoint { get; set; } = string.Empty;
            public string P256dh { get; set; } = string.Empty;
            public string Auth { get; set; } = string.Empty;
        }

        [HttpGet("public-key")]
        [AllowAnonymous]
        public IActionResult GetPublicKey(
            [FromServices] IConfiguration configuration)
        {
            var publicKey = configuration["Vapid:PublicKey"];

            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return StatusCode(500, new
                {
                    message = "VAPID public key is not configured."
                });
            }

            return Ok(new { publicKey });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromBody] PushSubscriptionRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Не удалось определить пользователя из JWT."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Endpoint) ||
                string.IsNullOrWhiteSpace(request.P256dh) ||
                string.IsNullOrWhiteSpace(request.Auth))
            {
                return BadRequest(new
                {
                    message = "Не заполнены данные push-подписки."
                });
            }

            if (!Uri.TryCreate(
                    request.Endpoint,
                    UriKind.Absolute,
                    out var endpointUri) ||
                endpointUri.Scheme != Uri.UriSchemeHttps)
            {
                return BadRequest(new
                {
                    message = "Некорректный endpoint подписки."
                });
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var userExists = await db.Users
                .AnyAsync(u => u.Id == userId);

            if (!userExists)
            {
                return Unauthorized();
            }

            var subscription = await db.PushSubscriptions
                .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint);

            if (subscription == null)
            {
                subscription = new PushSubscription
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    P256dh = request.P256dh,
                    Auth = request.Auth
                };

                db.PushSubscriptions.Add(subscription);
            }
            else
            {
                subscription.UserId = userId;
                subscription.P256dh = request.P256dh;
                subscription.Auth = request.Auth;
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Push-подписка сохранена."
            });
        }

        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> Unsubscribe(
            [FromBody] UnsubscribeRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest();
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var subscription = await db.PushSubscriptions
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId &&
                    p.Endpoint == request.Endpoint);

            if (subscription != null)
            {
                db.PushSubscriptions.Remove(subscription);
                await db.SaveChangesAsync();
            }

            return Ok(new { message = "Подписка удалена." });
        }

        public class UnsubscribeRequest
        {
            public string Endpoint { get; set; } = string.Empty;
        }
    }
}
