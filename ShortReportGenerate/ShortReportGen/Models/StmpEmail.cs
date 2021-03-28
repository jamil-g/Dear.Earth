using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using ShortReportGen.Models;

namespace ShortReportGen
{
    public class StmpEmail
    {
        #region property definition  
        #region property members definition
        #endregion

        #region consts members definition
        public readonly string c_StmpAddr = "smtp.gmail.com";
        public readonly string c_StmpUser = "report@dera.earth";
        public readonly string c_StmpPassword = "Report@2021";
        #endregion

        #endregion

        #region public methods
        public async Task SendEmailAsync(EmailInfo emailinfo)
        {

            // let's define the stmp parameters
            var smtpClient = new SmtpClient(c_StmpAddr)
            {
                Port = 587,
                UseDefaultCredentials=false,
                EnableSsl = true,
                Credentials = new NetworkCredential(c_StmpUser, c_StmpPassword),
            };

            // let's set the message info parameters
            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailinfo.Sender),
                Subject = emailinfo.Subject,
                Body = emailinfo.EmailMsg,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(emailinfo.Recipients);

            // let's add the message attachment
            var attachment = new Attachment(emailinfo.Attachment, MediaTypeNames.Image.Jpeg);
            mailMessage.Attachments.Add(attachment);
            smtpClient.Send(mailMessage);
            await Task.Delay(3000);
        }
        #endregion
    }
}
