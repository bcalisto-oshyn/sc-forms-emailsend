using NSubstitute;
using Oshyn.Modules.Forms.EmailSubmitAction.Models;
using Oshyn.Modules.Forms.EmailSubmitAction.Tests.Invoke;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc.Models.Fields;
using Sitecore.ExperienceForms.Processing;
using Sitecore.FakeDb;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using Xunit;

namespace Oshyn.Modules.Forms.EmailSubmitAction.Tests
{
    public class SendEmailActionTests
    {
        [Fact]
        public void GetSmtpClient_ConfigInField()
        {
            using (SetUpSettings())
            {
                var fieldContent =
                    "<Host>smtp.test.com</Host><Port>80</Port><Login>example@test.com</Login><Password>password</Password><EnableSsl>true</EnableSsl>";

                var submitActionDataMock = Substitute.For<ISubmitActionData>();
                var invokeObj = new InvokeSendEmailAction(submitActionDataMock);

                var smtpData = invokeObj.InvokeParseSmtpSettings(fieldContent);
                var smtpClient = invokeObj.InvokeGetSmtpClient(smtpData);

                Assert.Equal("smtp.test.com", smtpClient.Host);
                Assert.True(smtpClient.EnableSsl);
                Assert.Equal(80, smtpClient.Port);
            }
        }

        [Fact]
        public void GetSmtpClient_ConfigInPatch()
        {
            using (SetUpSettings())
            {
                var submitActionDataMock = Substitute.For<ISubmitActionData>();
                var invokeObj = new InvokeSendEmailAction(submitActionDataMock);

                var smtpClient = invokeObj.InvokeGetSmtpClient(new CustomSmtpModel());

                Assert.Equal("smtp.office365.com", smtpClient.Host);
                Assert.False(smtpClient.EnableSsl);
                Assert.Equal(587, smtpClient.Port);
            }
        }

        [Fact]
        public void ReplaceTokens_FromFormData()
        {
            var submitActionDataMock = Substitute.For<ISubmitActionData>();
            var formData = new FormSubmitContext("fakeButton");
            var today = DateTime.Today;

            formData.Fields = new List<IViewModel>
            {
                new StringInputViewModel { Name = "Text", Value = "Sample Simple Text" },
                new NumberViewModel { Name = "Number", Value = 1.55 },
                new ListBoxViewModel { Name = "Single List", Value = new List<string> { "Value 1" } },
                new ListBoxViewModel { Name = "Multiple List", Value = new List<string> { "Value 1", "Value 2" } },
                new DropDownListViewModel { Name = "Dropdown", Value = new List<string> { "Value 1" } },
                new CheckBoxViewModel { Name = "Checkbox", Value = true },
                new CheckBoxListViewModel { Name = "Checkbox List", Value = new List<string> { "Check 1", "Check 3" } },
                new DateViewModel { Name = "Date", Value = today }
            };

            var tokenText =
                "[Text]; [Number]; [Single List]; [Multiple List]; [Dropdown]; [Checkbox]; [Checkbox List]; [Date]; This should not be replaced.";

            var expectedText =
                $"Sample Simple Text; 1.55; Value 1; Value 1, Value 2; Value 1; True; Check 1, Check 3; {today}; This should not be replaced.";

            var invokeObj = new InvokeSendEmailAction(submitActionDataMock);
            var replacedText = invokeObj.InvokeReplaceTokens(tokenText, formData.Fields);

            Assert.Equal(expectedText, replacedText);
        }

        [Fact]
        public void AddEmailAddresses_WithValidAddresses()
        {
            var submitActionDataMock = Substitute.For<ISubmitActionData>();
            var mailMessage = new MailMessage();
            var fieldContent = "example@test.com; invalid; example2@test.com";

            var invokeObj = new InvokeSendEmailAction(submitActionDataMock);
            var addressesAdded = invokeObj.InvokeAddEmailAddresses(fieldContent, "To", mailMessage);

            Assert.True(addressesAdded);
        }

        [Fact]
        public void AddEmailAddresses_AllInvalidAddresses()
        {
            var submitActionDataMock = Substitute.For<ISubmitActionData>();
            var mailMessage = new MailMessage();
            var fieldContent = "invalidaddress; anotherinvalid";

            var invokeObj = new InvokeSendEmailAction(submitActionDataMock);
            var addressesAdded = invokeObj.InvokeAddEmailAddresses(fieldContent, "To", mailMessage);

            Assert.False(addressesAdded);
        }

        private Db SetUpSettings()
        {
            var db = new Db();

            db.Configuration.Settings["MailServer"] = "smtp.office365.com";
            db.Configuration.Settings["MailServerUserName"] = "donotreply@oshyn.com";
            db.Configuration.Settings["MailServerPassword"] = "doN0T58!#sRt*)";
            db.Configuration.Settings["MailServerPort"] = "587";

            return db;
        }
    }
}
