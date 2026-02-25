using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Dino.Common.Helpers;

namespace Dino.Infra.Email
{
	public class EmailClientSettings
	{
		public string SmtpHost { get; set; }
		public int SmtpPort { get; set; }
		public string SmtpUser { get; set; }
		public string SmtpPassword { get; set; }
		public bool EnableSsl { get; set; }
		public MailAddress DefaultFromAddress { get; set; }
	}

	public class EmailClient : IEmailClient
	{
		private readonly SmtpClient _smtpClient;
		private readonly MailAddress _defaultFrom;

		/// <summary>
		/// Gets an email client.
		/// </summary>
		/// <param name="settings">The SMTP client settings.</param>
		public EmailClient(EmailClientSettings settings)
		{
			_defaultFrom = settings.DefaultFromAddress;

			_smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort);

			_smtpClient.UseDefaultCredentials = false;
			_smtpClient.EnableSsl = settings.EnableSsl;

			if (!settings.SmtpUser.IsNullOrEmpty())
		    {
				var credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);
		        _smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
		        _smtpClient.Credentials = credentials;
		    }
		}

        public void SendEmail(string[] to, string[] cc, string[] bcc, IEmailGenerator emailGenerator, MailAddress[] replyTo, 
							  bool isHtml = true, MailAddress from = null, IEnumerable<Attachment> attachments = null)
        {
            var mailMessage = CreateMessage(to, cc, bcc, emailGenerator, replyTo, isHtml, from, attachments);
            if (mailMessage != null)
            {
                _smtpClient.Send(mailMessage);
            }
		}

        public async Task SendEmailAsync(string[] to, string[] cc, string[] bcc, IEmailGenerator emailGenerator,
            MailAddress[] replyTo, bool isHtml = true, MailAddress from = null, IEnumerable<Attachment> attachments = null)
        {
            var mailMessage = CreateMessage(to, cc, bcc, emailGenerator, replyTo, isHtml, from, attachments);
            if (mailMessage != null)
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
        }

        private MailMessage CreateMessage(string[] to, string[] cc, string[] bcc, IEmailGenerator emailGenerator,
            MailAddress[] replyTo, bool isHtml = true, MailAddress from = null, IEnumerable<Attachment> attachments = null)
        {
            MailMessage mailMessage = null;

            if (to.Any(x => !x.IsNullOrEmpty()))
            {
                mailMessage = new MailMessage();

                mailMessage.From = from ?? _defaultFrom;

                if (to != null)
                {
                    to.Foreach(new Action<string>(x =>
                    {
                        if (!x.IsNullOrEmpty())
                        {
                            mailMessage.To.Add(new MailAddress(x));
                        }
                    }));
                }

                if (cc != null)
                {
                    cc.Foreach(new Action<string>(x =>
                    {
                        if (!x.IsNullOrEmpty())
                        {
                            mailMessage.CC.Add(new MailAddress(x));
                        }
                    }));
                }

                if (bcc != null)
                {
                    bcc.Foreach(new Action<string>(x =>
                    {
                        if (!x.IsNullOrEmpty())
                        {
                            mailMessage.Bcc.Add(new MailAddress(x));
                        }
                    }));
                }

                if (replyTo != null)
                {
                    replyTo.Foreach(x => mailMessage.ReplyToList.Add(x));
                }

                mailMessage.IsBodyHtml = isHtml;
                mailMessage.Subject = emailGenerator.BuildSubject();
                mailMessage.Body = emailGenerator.BuildMessage();

                if (attachments != null)
                {
                    attachments.Foreach(x => mailMessage.Attachments.Add(x));
                }
            }

            return mailMessage;
        }
    }
}
