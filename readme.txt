SITECORE 9 FORMS SEND EMAIL SUBMIT ACTION
By Oshyn, Inc.
Version 1.1.0 (01/31/2019)

This is a simple Send Email submit action for Sitecore 9 Forms, 
similar to the Send Email Message action in WFFM.

RELEASE NOTES

This release addresses a bug reported by Bjarne Duus where list-type
fields in the form were appearing as "System.Collections.Generic.List"
in the email message. Now it should display the selected value
(if single selection) or a comma-separated list of selected values
in the email message.

INSTALLATION

After installing the module, make sure to restart both the
Sitecore Client and the Sitecore Server.

SMTP CONFIGURATION

The module will look up for SMTP configuration in this order:

- In-Action: When creating the Send Email action, you can (optionally) 
enter your SMTP configuration in the "Custom SMTP" field. It uses the 
following configuration values (tags are case sensitive):
<Host>[IP address or name of your SMTP server]</Host>
<Port>[Port of your SMTP server]</Port>
<EnableSsl>[Set to True to enable TLS/SSL]</EnableSsl>
<Login>[Username to login to your server]</Login>
<Password>[Password to login to your server]</Password>

- MailSettings: If you have configured the following settings in your 
Sitecore.config (or patch) file, they will be used as the default SMTP 
configuration: "MailServer", "MailServerUserName", "MailServerPassword", 
"MailServerPort". If you require TLS/SSL connection, don't forget to include 
your Web.config file the following:
<configuration>
  <system.net>
    <smtp deliveryMethod="Network">
      <network enableSsl="true" />
    </smtp>
  </system.net>
</configuration>

- Web.config mailSettings: If you don't configure in-action or use Sitecore's 
mail configuration settings, the module will attempt using the SMTP 
configuration in Web.config using standard ASP.NET mailSettings:
<configuration>
  <system.net>
    <smtp deliveryMethod="Network">
      <network host="[IP address or name of your SMTP server]"
        port="[Your server port]"
        userName="[Username to login to the server]"
        password="[Password to login to the server]"
        enableSsl="[Set to true to use TLS/SSL]"
        defaultCredentials="false" />
    </smtp>
  </system.net>
</configuration>