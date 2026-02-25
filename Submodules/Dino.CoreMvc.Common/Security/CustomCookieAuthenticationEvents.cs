using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dino.CoreMvc.Common.Security
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var userPrincipal = context.Principal;

            if (userPrincipal != null && userPrincipal.Identity != null && userPrincipal.Identity.IsAuthenticated)
            {
                if (userPrincipal.Identity is ClaimsIdentity claimsIdentity)
                {
                    var expirationClaims = claimsIdentity.Claims.Where(x => x.Type.EndsWith("Expiration")).ToList();
                    foreach (var currExpirationClaim in expirationClaims)
                    {
                        var currRole = currExpirationClaim.Type.Replace("Expiration", "");
                        var expirationDateString = currExpirationClaim.Value;

                        if (!string.IsNullOrEmpty(expirationDateString) &&
                            DateTime.TryParse(expirationDateString, out var userExpiration) &&
                            userExpiration < DateTime.UtcNow)
                        {
                            // If role is expired, remove the claim
                            var roleClaim = claimsIdentity.Claims.FirstOrDefault(x => (x.Type == ClaimTypes.Role) && (x.Value == currRole));
                            if (roleClaim != null)
                            {
                                claimsIdentity.RemoveClaim(roleClaim);
                            }
                        }
                    }
                    

                    if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role))
                    {
                        // If no roles are left, reject the principal (sign out)
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                    else
                    {
                        await context.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), context.Properties);
                    }
                }
            }
        }
    }
}
