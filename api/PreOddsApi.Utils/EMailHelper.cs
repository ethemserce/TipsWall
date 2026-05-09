using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace PreOddsApi.Utils
{
    public class EMailHelper
    {
        public static void SendEmail(EMailContent eMailContent)
        {
            var msg = new MailMessage();
            var smtpClient = new SmtpClient(eMailContent.ServerAddress, eMailContent.Port);

            if (string.IsNullOrWhiteSpace(eMailContent.FromDisplayName))
            {
                msg.From = new MailAddress(eMailContent.From);
            }
            else
            {
                msg.From = new MailAddress(eMailContent.From, eMailContent.FromDisplayName);
            }

            foreach (var eMail in eMailContent.ToList)
            {
                msg.To.Add(new MailAddress(eMail));
            }

            msg.Subject = eMailContent.Subject;
            msg.Body = eMailContent.Content;
            msg.IsBodyHtml = true;

            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;

            var loginInfo = new NetworkCredential(eMailContent.From, eMailContent.Password);
            smtpClient.Credentials = loginInfo;
            smtpClient.Send(msg);
        }

        public static bool CheckEMail(string eMail)
        {
            try
            {
                new MailAddress(eMail);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class EMailContent
    {
        public EMailContent()
        {
            Port = 25;
        }
        public string From { get; set; }
        public List<string> ToList { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string Password { get; set; }
        public string FromDisplayName { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; }
    }
}
