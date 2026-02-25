using Dino.Mvc.Common.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Dino.Mvc.Common.Helpers
{
	public interface IClientSessionCookieHelper
	{
		/// <summary>
		/// Login the user for 3 months.
		/// </summary>
		/// <param name="userId">The user ID.</param>
		/// <param name="userData">Custom user-data (string).</param>
		/// <param name="response">The HTTP-Response.</param>
		void LoginSystemPersistent(int userId, string userData, HttpResponse response);

		/// <summary>
		/// Gets the logged-in user's ID.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		int? GetLoggedInUserId(HttpContext context);

		/// <summary>
		/// Gets the logged-in user's data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		SessionUserData GetLoggedInUserData(HttpContext context);

		/// <summary>
		/// Extends the client cookie's TTL by a given time in minutes.
		/// </summary>
		/// <param name="context">The HTTP-context.</param>
		/// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
		/// Meaning, 15 minutes will set the TTL to 15 minutes from this moment.</param>
		void ExtendCookieTtlFromNow(HttpContext context, int minutesToExtendFromNow);

		/// <summary>
		/// Kills the session in the cookie.
		/// <param name="response">The HTTP-Response.</param>
		/// </summary>
		void KillSessionInCookie(HttpResponse response);
	}

	public class ClientSessionCookieHelper : IClientSessionCookieHelper
	{
		private readonly ISessionCookieHelpers _cookieHelpers;
		private readonly IOptions<DinoClientSettings> _clientSettings;

		public ClientSessionCookieHelper(ISessionCookieHelpers cookieHelpers, IOptions<DinoClientSettings> clientSettings)
		{
			_cookieHelpers = cookieHelpers;
			_clientSettings = clientSettings;
		}

		/// <summary>
		/// Login the user for 3 months.
		/// </summary>
		/// <param name="userId">The user ID.</param>
		/// <param name="userData">Custom user-data (string).</param>
		/// <param name="response">The HTTP-Response.</param>
		public void LoginSystemPersistent(int userId, string userData, HttpResponse response)
		{
			var data = new SessionUserData
			{
				UserId = userId,
				UserData = userData
			};

			var cookieTtl = _clientSettings.Value.ClientCookieTtl;
			var httpsOnly = _clientSettings.Value.ClientCookieHttpsOnly;
			var clientCookieId = _clientSettings.Value.ClientCookieId;

			_cookieHelpers.SetEncryptedCookie(clientCookieId, JsonConvert.SerializeObject(data), response, cookieTtl, httpsOnly, true);
		}

		/// <summary>
		/// Gets the logged-in user's ID.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public int? GetLoggedInUserId(HttpContext context)
		{
			int? userId = null;

			var clientCookieId = _clientSettings.Value.ClientCookieId;
			var cookieData = _cookieHelpers.GetEncryptedCookie(clientCookieId, context);
			if (cookieData != null)
			{
				try
				{
					var userData = JsonConvert.DeserializeObject<SessionUserData>(cookieData);
					if (userData != null)
					{
						userId = userData.UserId;
					}
				}
				catch
				{
				}
			}

			return userId;
		}

		/// <summary>
		/// Gets the logged-in user's data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public SessionUserData GetLoggedInUserData(HttpContext context)
		{
			SessionUserData userData = null;

			var clientCookieId = _clientSettings.Value.ClientCookieId;
			var cookieData = _cookieHelpers.GetEncryptedCookie(clientCookieId, context);
			if (cookieData != null)
			{
				try
				{
					userData = JsonConvert.DeserializeObject<SessionUserData>(cookieData);
				}
				catch
				{
				}
			}

			return userData;
		}

		/// <summary>
		/// Extends the client cookie's TTL by a given time in minutes.
		/// </summary>
		/// <param name="context">The HTTP-context.</param>
		/// <param name="minutesToExtendFromNow">OPTIONAL: The number of minutes to extend the session's time-to-live FROM NOW.
		/// Meaning, 15 minutes will set the TTL to 15 minutes from this moment.</param>
		public void ExtendCookieTtlFromNow(HttpContext context, int minutesToExtendFromNow)
		{
			var clientCookieId = _clientSettings.Value.ClientCookieId;
			_cookieHelpers.ExtendCookieTtlFromNow(clientCookieId, context, minutesToExtendFromNow);
		}

		/// <summary>
		/// Kills the session in the cookie.
		/// <param name="response">The HTTP-Response.</param>
		/// </summary>
		public void KillSessionInCookie(HttpResponse response)
		{
			var clientCookieId = _clientSettings.Value.ClientCookieId;
			_cookieHelpers.SetEncryptedCookie(clientCookieId, "", response);

			response.Cookies.Delete(clientCookieId);
		}
	}
}
