# Class: MicroM.DataDictionary.CategoriesValuesDef
## Overview
Schema definition for the category values table.

**Inheritance**
EntityDefinition -> CategoriesValuesDef

**Implements**
None

## Constructors
| Constructor | Description |
|:------------|:-------------|
| CategoriesValuesDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_category_id | Column&lt;string&gt; | Primary key referencing the category. |
| c_categoryvalue_id | Column&lt;string&gt; | Primary key for the category value identifier. |
| vc_description | Column&lt;string&gt; | Descriptive text for the value. |
| cav_brwStandard | ViewDefinition | Browse view keyed by category and value. |
| FKCategories | EntityForeignKey&lt;Categories, CategoriesValues&gt; | Links to the parent category. |
| UNDescription | EntityUniqueConstraint | Ensures descriptions are unique within a category. |

## Remarks
None.

