# EmailServiceQueueStatus

Tracks status values applied to queued emails.

## Columns
- `c_email_queue_id` (PK, FK to `EmailServiceQueue`)
- `c_status_id` (PK)
- `c_statusvalue_id` (FK to `StatusValues`)

## Relationships
- `FKQueue` – links to `EmailServiceQueue`.

## Typical Usage
Provides historical status information for items in the email queue.
