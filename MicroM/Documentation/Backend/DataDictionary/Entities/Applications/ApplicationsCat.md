# ApplicationsCat

Associates applications with category values.

## Columns
- `c_application_id` (PK, FK to `Applications`)
- `c_category_id` (PK)
- `c_categoryvalue_id` (FK to `CategoriesValues`)

## Relationships
- `FKApplicationsCat` – links to the parent `Applications` record.
- `FKCategories` – links to `CategoriesValues`.

## Typical Usage
Used to tag applications with additional category information.
