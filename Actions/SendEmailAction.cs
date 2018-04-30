using Oshyn.Modules.Forms.EmailSubmitAction.Models;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Processing.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Oshyn.Modules.Forms.EmailSubmitAction.Actions
{
    public class SendEmailAction : SubmitActionBase<SendEmailModel>
    {
        public SendEmailAction(ISubmitActionData submitActionData) : base(submitActionData) { }

        protected override bool Execute(SendEmailModel data, FormSubmitContext formSubmitContext)
        {
            Assert.ArgumentNotNull(formSubmitContext, "formSubmitContext");

            string fromValue = ReplaceTokens(data.From, formSubmitContext.Fields);
            string toValue = ReplaceTokens(data.To, formSubmitContext.Fields);
            string ccValue = ReplaceTokens(data.Cc, formSubmitContext.Fields);
            string bccValue = ReplaceTokens(data.Bcc, formSubmitContext.Fields);
            string subjectValue = ReplaceTokens(data.Subject, formSubmitContext.Fields);
            string messageValue = ReplaceTokens(data.Message, formSubmitContext.Fields);

            if (string.IsNullOrWhiteSpace(fromValue))
            {
                Logger.LogError("Send Email Action Error: No FROM address specified.");
                return false;
            }

            MailMessage mailMessage;

            try
            {
                mailMessage = new MailMessage
                {
                    From = new MailAddress(fromValue),
                    Subject = !string.IsNullOrWhiteSpace(subjectValue) ? subjectValue : "(No subject)",
                    Body = messageValue,
                    IsBodyHtml = data.IsHtml
                };
            }
            catch (FormatException ex)
            {
                Logger.LogError($"Send Email Action Error: {fromValue} in the FROM field is not a valid email address.", ex, this);
                return false;
            }

            if (!AddEmailAddresses(toValue, "To", mailMessage))
            {
                Logger.LogError("Send Email Action Error: No TO address(es) specified.");
                return false;
            }

            AddEmailAddresses(ccValue, "CC", mailMessage);
            AddEmailAddresses(bccValue, "Bcc", mailMessage);

            try
            {
                var customSmtp = ParseSmtpSettings(data.CustomSmtpConfig);
                var smtpClient = GetSmtpClient(customSmtp);
                smtpClient.Send(mailMessage);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Send Email Action Error: {ex.Message}", ex, this);
                return false;
            }
        }

        private SmtpClient GetSmtpClient(CustomSmtpModel customSmtp)
        {
            var smtpClient = new SmtpClient();

            string smtpServerSetting = Settings.GetSetting("MailServer");
            string smtpUserNameSetting = Settings.GetSetting("MailServerUserName");
            string smtpPasswordSetting = Settings.GetSetting("MailServerPassword");
            int smtpPortSetting = Settings.GetIntSetting("MailServerPort", 0);

            if (!string.IsNullOrWhiteSpace(smtpServerSetting))
                smtpClient.Host = smtpServerSetting;

            //Custom SMTP Host override
            if (!string.IsNullOrWhiteSpace(customSmtp.Host))
                smtpClient.Host = customSmtp.Host;

            if (!string.IsNullOrWhiteSpace(smtpUserNameSetting))
            {
                var credentials = new NetworkCredential(smtpUserNameSetting, !string.IsNullOrWhiteSpace(smtpPasswordSetting) ? smtpPasswordSetting : string.Empty);
                smtpClient.Credentials = credentials;
            }

            //Custom SMTP credentials (Login, Password) override
            if (!string.IsNullOrWhiteSpace(customSmtp.Login))
            {
                var credentials = new NetworkCredential(customSmtp.Login, !string.IsNullOrWhiteSpace(customSmtp.Password) ? customSmtp.Password : string.Empty);
                smtpClient.Credentials = credentials;
            }

            if (smtpPortSetting > 0)
                smtpClient.Port = smtpPortSetting;

            //Custom SMTP Port override
            if (customSmtp.Port > 0)
                smtpClient.Port = customSmtp.Port;

            //Custom SMTP EnableSsl override
            if (customSmtp.EnableSsl != null)
                smtpClient.EnableSsl = customSmtp.EnableSsl.Value;

            return smtpClient;
        }

        private bool AddEmailAddresses(string addressValue, string fieldName, MailMessage mailMessage)
        {
            string[] addressList = addressValue.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var addressCollection = mailMessage.GetType().GetProperty(fieldName).GetValue(mailMessage) as MailAddressCollection;

            if (addressList?.Length > 0)
            {
                foreach (string address in addressList)
                {
                    try
                    {
                        var addressToAdd = new MailAddress(address);
                        addressCollection.Add(addressToAdd);
                    }
                    catch
                    {
                        Logger.Warn($"Send Email Action Warning: {address} in the {fieldName.ToUpperInvariant()} field is not a valid email address.");
                        continue;
                    }
                }
            }

            if (addressCollection.Count > 0)
                return true;

            return false;
        }

        private string ReplaceTokens(string template, IList<IViewModel> formFields)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            var tokenList = Regex.Matches(template, @"\[(.+?)\]");
            var result = template;

            if (tokenList != null && tokenList.Count > 0)
            {
                var usedTokens = new Dictionary<string, bool>();

                foreach (Match token in tokenList)
                {
                    if (usedTokens.ContainsKey(token.Value))
                        continue;

                    var tokenName = token.Value.TrimStart('[').TrimEnd(']');
                    var matchingField = formFields.FirstOrDefault(f => f.Name == tokenName);

                    if (matchingField != null)
                    {
                        string fieldValue = GetFieldStringValue(matchingField);
                        result = result.Replace(token.Value, fieldValue);
                    }

                    usedTokens.Add(token.Value, true);
                }
            }

            return result;
        }

        private string GetFieldStringValue(object field)
        {
            return field?.GetType().GetProperty("Value")?.GetValue(field, null).ToString() ?? string.Empty;
        }

        private CustomSmtpModel ParseSmtpSettings(string settingsField)
        {
            var customSmtp = new CustomSmtpModel();

            if (!string.IsNullOrWhiteSpace(settingsField))
            {
                string finalXml = $"<CustomSmtpModel>{settingsField}</CustomSmtpModel>";
                var serializer = new XmlSerializer(typeof(CustomSmtpModel));

                using (var reader = new StringReader(finalXml))
                {
                    customSmtp = (CustomSmtpModel)serializer.Deserialize(reader);
                }
            }
            
            return customSmtp;
        }
    }
}