using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;

namespace Dino.Infra.Email
{
    public abstract class BaseEmailSender
    {
        private static string _baseEmailsPath = null;
        private static EmailClientSettings _settings = null;

        #region Init

        protected static void Init(string baseEmailsPath, string smtpHost, int smtpPort, string smtpUser,
                                  string smtpPassword, string defaultFromAddress, string defaultFromDisplayName)
        {
            _baseEmailsPath = baseEmailsPath;

            _settings = new EmailClientSettings
            {
                SmtpHost = smtpHost,
                SmtpPort = smtpPort,
                SmtpUser = smtpUser,
                SmtpPassword = smtpPassword,
                DefaultFromAddress = new MailAddress(defaultFromAddress, defaultFromDisplayName),
                EnableSsl = true
            };
        }

        #endregion

        #region Helpers

        protected static EmailClient GetEmailClient()
        {
            return new EmailClient(_settings);
        }

        protected static string GetEmailPath(string emailName)
        {
            var emailPath = Path.Combine(_baseEmailsPath, emailName);

            return emailPath;
        }

        protected static string GetEmailContent(string emailPath, Dictionary<string, string> values)
        {
            var emailContent = File.ReadAllText(emailPath);
            var messageBuilder = new EmailMessageBuilder();
            emailContent = messageBuilder.BuildMessage(emailContent, values);

            return emailContent;
        }

        #endregion
    }
}
