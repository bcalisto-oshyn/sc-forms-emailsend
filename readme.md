# Sitecore Forms Send Email Submit Action

This is a simple Send Email submit action for Sitecore 9 Forms, similar to the Send Email Message action in WFFM.

## Installation

- Download the .zip package from the Releases section, or from the Sitecore Marketplace at https://marketplace.sitecore.net/Modules/S/Sitecore_Forms_Send_Email_Submit_Action.aspx
- Use Sitecore's Installation Wizard to install the .zip package.
- When the installation ends, make sure you check both **Restart Sitecore Client** and **Restart Sitecore Server**

## Configuration

To configure your SMTP server, you have the following options that will be evaluated in this order of precedence:

- In-Action (using the *Custom SMTP* field in the action)
- Sitecore MailSettings in patch file
- Web.config .NET mail settings

### In-Action

When creating the Send Email action in your form, you can enter your SMTP configuration directly in the action in the *Custom SMTP* field. The configuration tags are similar to the ones used in WFFM, but these are case-sensitive:

```xml
<Host>[IP address or name of your SMTP server]</Host>
<Port>[Port number of your SMTP server]</Port>
<EnableSsl>[Set to True (with capital T) to enable TLS/SSL, omit to disable]</EnableSsl>
<Login>[Username to login to your SMTP server]</Login>
<Password>[Password to login to your SMTP server]</Password>
```

### Sitecore MailSettings

If you don't want to use the in-action configuration, you can create a Sitecore patch file (any .config file under `App_Config\Include`) with the following settings (these are the standard Sitecore SMTP settings):

```xml
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/" xmlns:set="http://www.sitecore.net/xmlconfig/set/">
  <sitecore>
    <settings>
      <setting name="MailServer" set:value="[IP address or name of your SMTP server]" />
      <setting name="MailServerPort" set:value="[Port number of your SMTP server]" />
      <setting name="MailServerUserName" set:value="[Username to login to your SMTP server]" />
      <setting name="MailServerPassword" set:value="[Password to login to your SMTP server]" />
    </settings>
  </sitecore>
</configuration>
```

If your server requires TLS/SSL, you need to modify your Sitecore installation's main Web.config file and add the `/configuration/system.net/smtp` section:

```xml
<configuration>
  <system.net>
    <smtp deliveryMethod="Network">
      <network enableSsl="true" />
    </smtp>
  </system.net>
</configuration>
```

### Web.config .NET mail settings

If you don't want to use the in-action configuration or MailSettings, you can use the standard .NET mail settings in your Sitecore installation's main Web.config file. You have to add the following `/configuration/system.net/smtp` section:

```xml
<configuration>
  <system.net>
    <smtp deliveryMethod="Network">
      <network host="[IP address or name of your SMTP server]"
        port="[Port number of your SMTP server]"
        userName="[Username to login to your SMTP server]"
        password="[Password to login to your SMTP server]"
        enableSsl="[Set to true to use SSL/TLS, set to false or omit to disable]"
        defaultCredentials="false" />
    </smtp>
  </system.net>
</configuration>
```

## Usage

Data from your Sitecore Form fields can be used for any of the Send Email Action fields. Just enclose the Field name into square brackets. For example, if you have a field called "Customer Name" in your form, you refer to it with the token `[Customer Name]` in your action field(s). This follows the same convention used in WFFM. If you scroll down in the Action configuration window, you will see a list of *valid* field tokens you can use.

Single-value fields will render whatever the user has entered in those values. Multiple-value fields (such as Checkbox Lists) will render the selected values comma-separated.
