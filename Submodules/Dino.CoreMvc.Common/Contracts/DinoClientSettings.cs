namespace Dino.Mvc.Common.Contracts
{
	public class DinoClientSettings
	{
		public string ClientCookieId { get; set; }
		public int ClientCookieTtl { get; set; }
		public bool ClientCookieHttpsOnly { get; set; }
	}
}
