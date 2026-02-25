using System.Collections.Generic;
using System.Net.Mail;

namespace Dino.Infra.Email
{
	public interface IEmailClient
	{
		/// <summary>
		/// Sends an email message
		/// </summary>
		/// <param name="to">The email addresses to send the message to.</param>
		/// <param name="cc">The CC email addresses to send the message to.</param>
		/// <param name="bcc">The BCC email addresses to send the message to.</param>
		/// <param name="emailGenerator">The message builder to use.</param>
		/// <param name="replyTo">The reply to email addresses.</param>
		/// <param name="isHtml">Is the message body in html code.</param>
		/// <param name="from">The address that the message will be sent from.</param>
		/// <param name="attachments">Mail attachments.</param>
		void SendEmail(string[] to, string[] cc, string[] bcc, IEmailGenerator emailGenerator, MailAddress[] replyTo, 
					   bool isHtml = true, MailAddress from = null, IEnumerable<Attachment> attachments = null);
	}
}
