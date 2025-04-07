import { DefaultColumns, EntityColumn, EntityColumnFlags, EntityDefinition, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_email_configuration_id: new EntityColumn<string>({ name: 'c_email_configuration_id', type: 'char', length: 20, flags: c.PK, prompt: 'Email Configuration Id'}),
        vc_smtp_host: new EntityColumn<string>({
            name: 'vc_smtp_host', type: 'varchar', length: 2048, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Smtp Host',
            description: 'The host of the smtp server', placeholder: 'smtp.gmail.com'
        }),
        i_smtp_port: new EntityColumn<number>({
            name: 'i_smtp_port', type: 'int', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Smtp Port',
            description: 'The port of the smtp server', placeholder: '465'
        }),
        vc_user_name: new EntityColumn<string>({
            name: 'vc_user_name', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Smtp Username',
            description: 'The username for the smtp server'
        }),
        vc_password: new EntityColumn<string>({
            name: 'vc_password', type: 'varchar', length: 2048, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Smtp Password',
            description: 'The password for the smtp server'
        }),
        bt_use_ssl: new EntityColumn<boolean>({
            name: 'bt_use_ssl', type: 'bit', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Use Ssl',
            description: 'Use ssl for the smtp server'
        }),
        vc_default_sender_email: new EntityColumn<string>({
            name: 'vc_default_sender_email', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Default Sender Email',
            description: 'The default sender email address'
        }),
        vc_default_sender_name: new EntityColumn<string>({
            name: 'vc_default_sender_name', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Default Sender Name',
            description: 'The default sender name'
        }),
        vc_template_subject: new EntityColumn<string>({
            name: 'vc_template_subject', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Subject',
            description: 'The subject for the recovery email',
            defaultValue: 'Password recovery',
            placeholder: 'Password recovery',
        }),
        vc_template_body: new EntityColumn<string>({
            name: 'vc_template_body', type: 'varchar', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Message',
            description: 'The message body for the recovery email',
            defaultValue: 'You have requested an email to recover your password.\n\nPlease follow this link to set your new password: https://<YOUR_SITE>/recover/?code={RECOVERY_CODE}\n\nThe support team.',
            placeholder: 'You have requested an email to recover your password.\n\nPlease follow this link to set your new password: https://<YOUR_SITE>/recover/?code={RECOVERY_CODE}\n\nThe support team.',
        }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        eqc_brwStandard: { name: 'eqc_brwStandard', keyMappings: { c_email_configuration_id: 0 } }
    }
)

export class EmailServiceConfigurationDef extends EntityDefinition {

    columns = columns();
    views = views();

    constructor() {
        super('EmailServiceConfiguration');
    }
}