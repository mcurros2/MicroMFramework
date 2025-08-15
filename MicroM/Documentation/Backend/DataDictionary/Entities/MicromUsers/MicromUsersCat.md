# MicromUsersCat

Associates users with category values.

## Columns
- `c_user_id` (PK, FK to `MicromUsers`)
- `c_category_id` (PK)
- `c_categoryvalue_id` (FK to `CategoriesValues`)

## Relationships
- `FKMicromUsers` – links to `MicromUsers`.
- `FKCategories` – links to `CategoriesValues`.

## Typical Usage
Used to tag users with category metadata such as roles or custom flags.
