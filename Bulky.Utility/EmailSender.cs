using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class EmailSender : IEmailSender
    {
        /* Another way to access the secretKey in contrast with Stripe */
        public string SendGridSecret { get; set; }
        public EmailSender(IConfiguration _config) 
        {
            SendGridSecret = _config.GetValue<string>("SendGrid:SecretKey");
        }    
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            //logic here to se email
            var client = new SendGridClient(SendGridSecret);

            //We have to write the email which i have used to verify the single sender Identity (SOS domain email to Work)
            var from = new EmailAddress("hello@dotmastery.com", "Bulk Book");
            var to = new EmailAddress(email);
            var message = MailHelper.CreateSingleEmail(from, to,subject,"",htmlMessage);

            return client.SendEmailAsync(message);


        }
    }
}
