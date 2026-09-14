import type { Entity, EntityDefinition, EntityLookup } from "../../Entity";

export interface LookupKeyColumnResolution {
    columnName?: string;
    error?: string;
}

export function resolveLookupKeyColumn(
    lookupDef: EntityLookup,
    lookupEntity: Entity<EntityDefinition>,
    viewName: string,
    bindingColumn: string
): LookupKeyColumnResolution {
    const validateColumn = (columnName: string, source: string): LookupKeyColumnResolution => {
        if (lookupEntity.def.columns[columnName]) return { columnName };

        return {
            error: `Lookup '${lookupDef.name}' ${source} resolves to column '${columnName}', but that column does not exist in lookup entity '${lookupEntity.def.name}'.`
        };
    };

    if (lookupDef.bindingColumnKey) {
        return validateColumn(lookupDef.bindingColumnKey, 'bindingColumnKey');
    }

    const view = lookupEntity.def.views[viewName];
    if (view) {
        const keyIndex = lookupDef.viewMapping?.keyIndex ?? 0;
        const mappedColumns = Object.entries(view.keyMappings)
            .filter(([, resultIndex]) => resultIndex === keyIndex)
            .map(([columnName]) => columnName);

        if (mappedColumns.length === 1) {
            return validateColumn(mappedColumns[0], `view '${viewName}' key mapping at result index ${keyIndex}`);
        }

        if (mappedColumns.length > 1) {
            return {
                error: `Lookup '${lookupDef.name}' view '${viewName}' has multiple key mappings at result index ${keyIndex}. Configure bindingColumnKey explicitly.`
            };
        }
    }

    if (lookupEntity.def.columns[bindingColumn]) return { columnName: bindingColumn };

    return {
        error: view
            ? `Lookup '${lookupDef.name}' cannot infer its key column: view '${viewName}' has no key mapping for result index ${lookupDef.viewMapping?.keyIndex ?? 0}, and lookup entity '${lookupEntity.def.name}' has no column named '${bindingColumn}'.`
            : `Lookup '${lookupDef.name}' cannot infer its key column: view '${viewName}' was not found in lookup entity '${lookupEntity.def.name}', and the entity has no column named '${bindingColumn}'.`
    };
}
