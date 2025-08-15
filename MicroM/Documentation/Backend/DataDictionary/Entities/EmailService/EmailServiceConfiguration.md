# EmailServiceConfiguration

SMTP settings used by the email service.

## Columns
- `c_email_configuration_id` (PK)
- `vc_smtp_host`
- `i_smtp_port`
- `vc_user_name`
- `vc_password` – encrypted
- `bt_use_ssl`
- `vc_default_sender_email`
- `vc_default_sender_name`
- `vc_template_subject` (fake)
- `vc_template_body` (fake)

## Typical Usage
Holds SMTP credentials and default sender information for outbound email.
