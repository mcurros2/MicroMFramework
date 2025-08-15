# EmailServiceQueue

Queue of emails awaiting delivery.

## Columns
- `c_email_queue_id` (PK)
- `c_email_configuration_id` (FK to `EmailServiceConfiguration`)
- `vc_external_reference`
- `c_email_process_id`
- `vc_sender_email`
- `vc_sender_name`
- `vc_destination_email`
- `vc_destination_name`
- `vc_subject`
- `vc_message`
- `vc_last_error`
- `i_retries`
- `c_emailstatus_id` – status identifier
- `vc_json_destination_and_tags` (fake)
- `c_email_template_id` (FK, fake)

## Relationships
- `FKConfiguration` – links to `EmailServiceConfiguration`.
- Indexes on process identifiers.

## Typical Usage
Emails are inserted here and later processed by the email worker. Procedures support queuing messages and templates.
