using MailKit.Net.Smtp;
using MimeKit;
using NospamHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NospamHelper
{
    public class MailHelper
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailHelper> _logger;
        private SmtpClient _smtpClient;
        public MailHelper(IConfiguration Config, ILogger<MailHelper> Logger)
        {
            _config = Config;
            _logger = Logger;

            Init();
        }

        private void Init()
        {
            _smtpClient = new SmtpClient();
        }

        internal void SendNotification (LargeFileEntry File)
        {
            MimeMessage message = new MimeMessage()
            {
                Subject = $"File Approved: {File.Name}",
                Body = new TextPart(MimeKit.Text.TextFormat.Plain)
                {
                    Text = $"The file {File.Name} was automatic checked"
                }
            };
            message.To.Add(new MailboxAddress(_config.GetValue<string>("ToMail"), _config.GetValue<string>("ToMail")));
            message.From.Add(new MailboxAddress(_config.GetValue<string>("FromMail"), _config.GetValue<string>("FromMail")));

            _smtpClient.Connect(_config.GetValue<string>("MailServer"), _config.GetValue<int>("MailServerPort"));
            _logger.LogInformation($"Notification was send for {File.Name} to {_config.GetValue<string>("ToMail")}");
            _smtpClient.Send(message);
            _smtpClient.Disconnect(true);
        }
    }
}
