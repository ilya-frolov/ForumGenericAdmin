using Dino.Common.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Dino.CoreMvc.Common.Helpers
{
    public static class AccountHelpers
    {
        #region LoginToRole

        public static async Task LoginToRole(HttpContext httpContext, string role, string idContainer, string id, bool rememberMe,
            bool keepExisting, Dictionary<string, string> additionalData = null, TimeSpan? roleCustomExpiration = null)
        {
            var roleIdPairs = new List<(string Role, string IdContainer)> { (role, idContainer) };

            var claims = CreateBaseClaims(roleIdPairs, id, additionalData, roleCustomExpiration);

            AddExistingClaims(httpContext, claims, keepExisting, new List<string> { role });

            await SignInAsync(httpContext, claims, rememberMe);
        }

        #endregion

        #region LoginToMultipleRoles

        public static async Task LoginToMultipleRoles(HttpContext httpContext, List<(string Role, string IdContainer)> roleIdPairs, string id, bool rememberMe,
            bool keepExisting, Dictionary<string, string> additionalData = null, TimeSpan? roleCustomExpiration = null)
        {
            var claims = CreateBaseClaims(roleIdPairs, id, additionalData, roleCustomExpiration);

            var existingRoles = roleIdPairs.Select(p => p.Role).ToList();

            AddExistingClaims(httpContext, claims, keepExisting, existingRoles);

            await SignInAsync(httpContext, claims, rememberMe);
        }

        #endregion

        #region CreateBaseClaims

        private static List<Claim> CreateBaseClaims(List<(string Role, string IdContainer)> roleIdPairs, string id, Dictionary<string, string> additionalData,
            TimeSpan? roleCustomExpiration)
        {
            var claims = new List<Claim>();

            foreach (var (role, idContainer) in roleIdPairs)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

                if (roleCustomExpiration.HasValue)
                {
                    claims.Add(new Claim($"{role}Expiration", DateTime.UtcNow.Add(roleCustomExpiration.Value).ToString()));
                }

                if (!string.IsNullOrEmpty(id))
                {
                    claims.Add(new Claim(idContainer, id));
                }
            }

            if (additionalData != null)
            {
                foreach (var currData in additionalData)
                {
                    claims.Add(new Claim(currData.Key, currData.Value));
                }
            }

            return claims;
        }

        #endregion

        #region AddExistingClaims

        private static void AddExistingClaims(HttpContext httpContext, List<Claim> claims, bool keepExisting, List<string> existingRoles)
        {
            if (keepExisting && httpContext.User.Identity.IsAuthenticated)
            {
                foreach (var currClaim in httpContext.User.Claims)
                {
                    if (currClaim.Type != ClaimTypes.Role)
                    {
                        if (claims.All(x => x.Type != currClaim.Type))
                        {
                            claims.Add(currClaim);
                        }
                    }
                    else
                    {
                        if (!existingRoles.Contains(currClaim.Value))
                        {
                            claims.Add(currClaim);
                        }
                    }
                }
            }
        }

        #endregion

        #region SignInAsync

        private static async Task SignInAsync(HttpContext httpContext, List<Claim> claims, bool rememberMe)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = rememberMe
            });
        }

        #endregion

        #region ExtendRoleExpiration

        public static async Task ExtendRoleExpiration(HttpContext httpContext, string role, TimeSpan roleCustomExpiration, bool rememberMe)
        {
            if (httpContext.User.Identity.IsAuthenticated)
            {
                if (httpContext.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    var expirationClaim = claimsIdentity.Claims.FirstOrDefault(x => x.Type == $"{role}Expiration");
                    if (expirationClaim != null)
                    {
                        claimsIdentity.RemoveClaim(expirationClaim);
                    }

                    claimsIdentity.AddClaim(new Claim($"{role}Expiration", DateTime.UtcNow.Add(roleCustomExpiration).ToString()));

                    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties
                    {
                        IsPersistent = rememberMe
                    });
                }
            }
        }

        #endregion

        #region LogoutRole

        public static async Task LogoutRole(HttpContext httpContext, string role, string idContainer)
        {
            // Get the current user's claims
            var userClaims = httpContext.User.Claims.ToList();

            // Find the claims associated with the specific role
            var roleClaims = userClaims.Where(c => ((c.Type == ClaimTypes.Role) && (c.Value == role)) || (c.Type == idContainer)).ToList();

            // Create a new claims identity without the role claims
            var remainingClaims = userClaims.Except(roleClaims).ToList();
            var newClaimsIdentity = new ClaimsIdentity(remainingClaims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign in with the new identity
            if (remainingClaims.Count > 0)
            {
                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(newClaimsIdentity));
            }
            else
            {
                await httpContext.SignOutAsync();
            }
        }

        #endregion
    }
}
