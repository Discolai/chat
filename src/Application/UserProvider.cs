using Core.User;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Application;

public class UserProvider(IHttpContextAccessor httpContextAccessor, IClusterClient clusterClient)
{
    public bool TryGetUserId([NotNullWhen(true)] out string? userId)
    {
        userId = null;
        var userPrincipal = httpContextAccessor.HttpContext?.User;
        if (userPrincipal is null) {
            return false;
        }

        userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId);
    }

    public bool TryGetUser([NotNullWhen(true)] out IUserGrain? user, [NotNullWhen(true)] out string? userId)
    {
        user = null;
        if (!TryGetUserId(out userId))
        {
            return false;
        }

        user = clusterClient.GetGrain<IUserGrain>(userId);
        return user is not null;
    }
}
