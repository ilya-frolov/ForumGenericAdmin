using System;
using System.Text;
using Dino.Common.Helpers;
using Dino.Mvc.Common.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Dino.Mvc.Common.Helpers
{
    public class SessionUserData
    {
        public int UserId { get; set; }
        public string UserData { get; set; }
    }

    public interface ISessionCookieHelpers
    {
	    /// <summary>
	    /// Sets a cookie with encrypted data.
	    /// </summary>
	    /// <param name="name">The cookie's name.</param>
	    /// <param name="value">The cookie's value.</param>
	    /// <param name="response">The HTTP-Response.</param>
	    /// <param name="ttlMinutes">The time-to-live, of the cookie, in minutes.</param>
	    /// <param name="httpsOnly">Should the cookie be available through HTTPS only</param>
	    /// <param name="serverOnly">Sets wether the cookie is available on server side only.</param>
	    void SetEncryptedCookie(string name, string value, HttpResponse response, int? ttlMinutes = null, bool httpsOnly = false, bool serverOnly = false);

	    /// <summary>
	    /// Gets and decrypts a cookie, and retreives the data.
	    /// </summary>
	    /// <param name="name">The cookie's name.</param>
	    /// <param name="request">The HTTP-Request.</param>
	    /// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
	    /// Meaning, 15 minutes will set the TTL to 15 minutes from this moment. NULL if not required.</param>
	    /// <returns>The cookie's value.</returns>
	    string GetEncryptedCookie(string name, HttpContext context, int? minutesToExtendFromNow = null);

	    /// <summary>
	    /// Extends the cookie's TTL by a given time in minutes.
	    /// </summary>
	    /// <param name="name">The cookie's name.</param>
	    /// <param name="context">The HTTP-context.</param>
	    /// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
	    /// Meaning, 15 minutes will set the TTL to 15 minutes from this moment.</param>
	    void ExtendCookieTtlFromNow(string name, HttpContext context, int minutesToExtendFromNow);
    }

    /// <summary>
    /// Summary description for CookieHelpers
    /// </summary>
    public class SessionCookieHelpers : ISessionCookieHelpers
    {
		private readonly IDataProtector _cookiesProtector;

		public const int COOKIE_LENGTH_DAYS = 90;

        public SessionCookieHelpers(IDataProtectionProvider protectionProvider)
        {
	        _cookiesProtector = protectionProvider.CreateProtector("CookiesProtect");
        }

        /// <summary>
        /// Sets a cookie with encrypted data.
        /// </summary>
        /// <param name="name">The cookie's name.</param>
        /// <param name="value">The cookie's value.</param>
        /// <param name="response">The HTTP-Response.</param>
        /// <param name="ttlMinutes">The time-to-live, of the cookie, in minutes.</param>
        /// <param name="httpsOnly">Should the cookie be available through HTTPS only</param>
        /// <param name="serverOnly">Sets wether the cookie is available on server side only.</param>
        public void SetEncryptedCookie(string name, string value, HttpResponse response, int? ttlMinutes = null, bool httpsOnly = false, bool serverOnly = false)
        {
	        var cookieText = Encoding.UTF8.GetBytes(value);
            var encryptedValue = Convert.ToBase64String(_cookiesProtector.Protect(cookieText));

            //--- Create cookie object and pass name of the cookie and value to be stored.
            var cookieOptions = new CookieOptions();

            //---- Set expiry time of cookie
            if (ttlMinutes.HasValue && (ttlMinutes.Value > 0))
            {
                cookieOptions.Expires = DateTime.Now.AddMinutes(ttlMinutes.Value);
            }
            else
            {
                cookieOptions.Expires = DateTime.Now.AddDays(COOKIE_LENGTH_DAYS);
            }

            if (httpsOnly)
            {
                cookieOptions.Secure = true;
            }

            if (serverOnly)
            {
                cookieOptions.HttpOnly = true;
            }

            //---- Add cookie to cookie collection.
            response.Cookies.Append(name, encryptedValue, cookieOptions);
        }

        /// <summary>
        /// Gets and decrypts a cookie, and retreives the data.
        /// </summary>
        /// <param name="name">The cookie's name.</param>
        /// <param name="request">The HTTP-Request.</param>
        /// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
        /// Meaning, 15 minutes will set the TTL to 15 minutes from this moment. NULL if not required.</param>
        /// <returns>The cookie's value.</returns>
        public string GetEncryptedCookie(string name, HttpContext context, int? minutesToExtendFromNow = null)
        {
            // Validate that the cookie exists. Otherwise, return empty string.
            string result = "";
            var cookie = context.Request.Cookies[name];
            if (cookie.IsNotNullOrEmpty() )
            {
                try
                {
                    var bytes = Convert.FromBase64String(cookie);
                    var output = _cookiesProtector.Unprotect(bytes);
                    result = Encoding.UTF8.GetString(output);

                    // Extend cookie.
                    if (minutesToExtendFromNow.HasValue)
                    {
                        ExtendCookieTtlFromNow(name, context, minutesToExtendFromNow.Value);
                    }
                }
                catch (Exception ex)
                {

                    //throw;
                }
            }

            return (result);
        }

        /// <summary>
        /// Extends the cookie's TTL by a given time in minutes.
        /// </summary>
        /// <param name="name">The cookie's name.</param>
        /// <param name="context">The HTTP-context.</param>
        /// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
        /// Meaning, 15 minutes will set the TTL to 15 minutes from this moment.</param>
        public void ExtendCookieTtlFromNow(string name, HttpContext context, int minutesToExtendFromNow)
        {
            var cookie = context.Request.Cookies[name];
            if (cookie != null)
            {
	            context.Response.Cookies.Append(name, cookie, new CookieOptions
                {
					Expires = DateTime.Now.AddMinutes(minutesToExtendFromNow)
				});
            }
        }

    }
}
