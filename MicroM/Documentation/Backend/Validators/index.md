# Validators

This section documents the regular expressions used by MicroM's backend validators and the scenarios they cover.

## URL Validator

- **Regex:** `^https?://[^\s]+[^\s]$` *(case-insensitive)*
- **Scenario:** Ensures a value is an HTTP or HTTPS URL without spaces.

## Phone Validator

- **Regex:** `^\+?\d+$`
- **Scenario:** Validates phone numbers consisting of digits with an optional leading `+`.

## Email Validator

- **Regex:** `/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/`
- **Scenario:** Confirms a value follows a standard email address format.

