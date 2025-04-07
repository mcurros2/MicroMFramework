import { IconMailCog } from "@tabler/icons-react";
import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { EmailServiceConfigurationDef } from "./EmailServiceConfigurationDef";

export const EmailServiceConfigurationIcon = IconMailCog;
export const EmailServiceConfigurationHelpText = '* Configure your email server settings.';
export const EmailServiceConfigurationTitle = 'Email Service Configuration';

export class EmailServiceConfiguration extends Entity<EmailServiceConfigurationDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new EmailServiceConfigurationDef(), parentKeys);
        this.Form = import('./EmailServiceConfigurationForm').then((module) => module.EmailServiceConfigurationForm);
        this.Title = EmailServiceConfigurationTitle;
        this.HelpText = EmailServiceConfigurationHelpText;
        this.Icon = EmailServiceConfigurationIcon;

        this.def.columns.c_email_configuration_id.value = client.getAPPID();
    }
}