# Class: MicroM.DataDictionary.CategoriesDef
## Overview
Schema definition for the Categories table.

**Inheritance**
EntityDefinition -> CategoriesDef

**Implements**
None

## Constructors
| Constructor | Description |
|:------------|:-------------|
| CategoriesDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_category_id | Column&lt;string&gt; | Primary key column for the category identifier. |
| vc_description | Column&lt;string&gt; | Descriptive name for the category. |
| cat_brwStandard | ViewDefinition | Standard browse view keyed by category ID. |

## Remarks
None.

