namespace Dino.Infra.Email
{
	public interface IEmailGenerator
	{
		/// <summary>
		/// Builds the email subject.
		/// </summary>
		/// <returns>The email's subject</returns>
		string BuildSubject();

		/// <summary>
		/// Builds the email message.
		/// </summary>
		/// <returns>The email's message</returns>
		string BuildMessage();
	}
}
