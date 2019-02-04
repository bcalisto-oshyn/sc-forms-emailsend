using Oshyn.Modules.Forms.EmailSubmitAction.Actions;
using Oshyn.Modules.Forms.EmailSubmitAction.Models;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using System.Collections.Generic;
using System.Net.Mail;

namespace Oshyn.Modules.Forms.EmailSubmitAction.Tests.Invoke
{
    internal class InvokeSendEmailAction : SendEmailAction
    {
        internal InvokeSendEmailAction(ISubmitActionData submitActionData) : base(submitActionData) { }

        internal bool InvokeExecute(SendEmailModel data, FormSubmitContext formSubmitContext)
        {
            return Execute(data, formSubmitContext);
        }

        internal SmtpClient InvokeGetSmtpClient(CustomSmtpModel customSmtp)
        {
            return GetSmtpClient(customSmtp);
        }

        internal bool InvokeAddEmailAddresses(string addressValue, string fieldName, MailMessage mailMessage)
        {
            return AddEmailAddresses(addressValue, fieldName, mailMessage);
        }

        internal string InvokeReplaceTokens(string template, IList<IViewModel> formFields)
        {
            return ReplaceTokens(template, formFields);
        }

        internal string InvokeGetFieldStringValue(object field)
        {
            return GetFieldStringValue(field);
        }

        internal CustomSmtpModel InvokeParseSmtpSettings(string settingsField)
        {
            return ParseSmtpSettings(settingsField);
        }
    }
}
