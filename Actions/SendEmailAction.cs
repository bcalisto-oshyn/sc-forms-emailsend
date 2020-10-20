using Oshyn.Modules.Forms.EmailSubmitAction.Models;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc.Models.Fields;
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

            var fromValue = ReplaceTokens(data.From, formSubmitContext.Fields);
            var toValue = ReplaceTokens(data.To, formSubmitContext.Fields);
            var ccValue = ReplaceTokens(data.Cc, formSubmitContext.Fields);
            var bccValue = ReplaceTokens(data.Bcc, formSubmitContext.Fields);
            var subjectValue = ReplaceTokens(data.Subject, formSubmitContext.Fields);
            var messageValue = ReplaceTokens(data.Message, formSubmitContext.Fields);

            if (string.IsNullOrWhiteSpace(fromValue))
            {
                Logger?.LogError("Send Email Action Error: No FROM address specified.");
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
                Logger?.LogError($"Send Email Action Error: {fromValue} in the FROM field is not a valid email address.", ex, this);
                return false;
            }

            if (!AddEmailAddresses(toValue, "To", mailMessage))
            {
                Logger?.LogError("Send Email Action Error: No TO address(es) specified.");
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
                Logger?.LogError($"Send Email Action Error: {ex.Message}", ex, this);
                return false;
            }
        }

        protected SmtpClient GetSmtpClient(CustomSmtpModel customSmtp)
        {
            var smtpClient = new SmtpClient();

            var smtpServerSetting = Settings.GetSetting("MailServer");
            var smtpUserNameSetting = Settings.GetSetting("MailServerUserName");
            var smtpPasswordSetting = Settings.GetSetting("MailServerPassword");
            var smtpPortSetting = Settings.GetIntSetting("MailServerPort", 0);
            var smtpSslSettingString = Settings.GetSetting("MailServerUseSsl", string.Empty);

            if (!string.IsNullOrWhiteSpace(smtpServerSetting))
                smtpClient.Host = smtpServerSetting;

            //Custom SMTP Host override
            if (!string.IsNullOrWhiteSpace(customSmtp.Host))
                smtpClient.Host = customSmtp.Host;

            if (!string.IsNullOrWhiteSpace(smtpUserNameSetting))
                smtpClient.Credentials = new NetworkCredential(smtpUserNameSetting, !string.IsNullOrWhiteSpace(smtpPasswordSetting) ? smtpPasswordSetting : string.Empty);

            //Custom SMTP credentials (Login, Password) override
            if (!string.IsNullOrWhiteSpace(customSmtp.Login))
                smtpClient.Credentials = new NetworkCredential(customSmtp.Login, !string.IsNullOrWhiteSpace(customSmtp.Password) ? customSmtp.Password : string.Empty);

            if (smtpPortSetting > 0)
                smtpClient.Port = smtpPortSetting;

            //Custom SMTP Port override
            if (customSmtp.Port > 0)
                smtpClient.Port = customSmtp.Port;

            //Custom SMTP EnableSsl override
            if (!string.IsNullOrWhiteSpace(smtpSslSettingString) && bool.TryParse(smtpSslSettingString, out bool smtpSslSetting))
                smtpClient.EnableSsl = smtpSslSetting;

            if (customSmtp.EnableSsl != null)
                smtpClient.EnableSsl = customSmtp.EnableSsl.Value;

            return smtpClient;
        }

        protected bool AddEmailAddresses(string addressValue, string fieldName, MailMessage mailMessage)
        {
            if (string.IsNullOrWhiteSpace(addressValue) || string.IsNullOrWhiteSpace(fieldName) || mailMessage == null)
                return false;

            var addressList = addressValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var addressCollection = mailMessage.GetType().GetProperty(fieldName)?.GetValue(mailMessage) as MailAddressCollection;

            if (addressList.Length <= 0 || addressCollection == null)
                return false;

            foreach (var address in addressList)
            {
                try
                {
                    var addressToAdd = new MailAddress(address);
                    addressCollection.Add(addressToAdd);
                }
                catch
                {
                    Logger?.Warn($"Send Email Action Warning: {address} in the {fieldName.ToUpperInvariant()} field is not a valid email address.");
                }
            }

            return addressCollection.Count > 0;
        }

        protected string ReplaceTokens(string template, IList<IViewModel> formFields)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            var tokenList = Regex.Matches(template, @"\[(.+?)\]");

            if (tokenList.Count <= 0)
                return template;

            var usedTokens = new Dictionary<string, bool>();

            foreach (Match token in tokenList)
            {
                if (usedTokens.ContainsKey(token.Value))
                    continue;

                var tokenName = token.Value.TrimStart('[').TrimEnd(']');
                var matchingField = formFields.FirstOrDefault(f => f.Name == tokenName);

                if (matchingField != null) 
                { 
                    template = template.Replace(token.Value, GetFieldStringValue(matchingField));
                }
                else
                {
                    template = template.Replace(token.Value, string.Empty);
                    Logger?.Warn($"Field {token.Value} not found in form, replacing by empty string.");
                }

                usedTokens.Add(token.Value, true);
            }

            return template;
        }

        protected string GetFieldStringValue(object field)
        {
            if (field != null && field is ListViewModel)
            {
                var listField = (ListViewModel)field;
                
                if (listField.Value == null || !listField.Value.Any())
                    return string.Empty;

                return string.Join(", ", listField.Value);
            }

            return field?.GetType().GetProperty("Value")?.GetValue(field, null)?.ToString() ?? string.Empty;
        }

        protected CustomSmtpModel ParseSmtpSettings(string settingsField)
        {
            var customSmtp = new CustomSmtpModel();

            if (string.IsNullOrWhiteSpace(settingsField))
                return customSmtp;

            var finalXml = $"<CustomSmtpModel>{settingsField}</CustomSmtpModel>";
            var serializer = new XmlSerializer(typeof(CustomSmtpModel));

            using (var reader = new StringReader(finalXml))
            {
                customSmtp = (CustomSmtpModel)serializer.Deserialize(reader);
            }

            return customSmtp;
        }
    }
}