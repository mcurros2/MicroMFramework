import { Value } from "../client";
import { EntityClientAction } from "./EntityClientAction";
import { EntityColumn } from "./EntityColumn";
import { EntityColumnOptions } from "./EntityColumn.types";
import { ColumnsObject } from "./EntityColumnCollection.types";
import { EntityLookup } from "./EntityLookup";
import { EntityProc } from "./EntityProc";
import { EntityServerAction } from "./EntityServerAction";
import { EntityView } from "./EntityView";



export class EntityDefinition {
    name;

    columns: ColumnsObject = {};
    procs: Record<string, EntityProc> = {};
    views: Record<string, EntityView> = {};
    lookups: Record<string, EntityLookup> = {};
    serverActions: Record<string, EntityServerAction> = {};
    clientActions: Record<string, EntityClientAction> = {};
    importColumns: string[] | null = null;

    standardView() {
        let result: string | null = null;
        for (const v in this.views) {
            if (v.toLowerCase().endsWith('brwstandard')) result = v;
        }
        return result;
    }

    constructor(name: string) {
        if (typeof name !== 'string' || name.length === 0) throw new Error('Invalid entity name.');
        this.name = name;
    };

    addCol<T extends Value>({ name, type, length, scale, value, defaultValue, flags, prompt }: EntityColumnOptions<T>) {
        const col = new EntityColumn({ name, type, length, scale, value, defaultValue, flags, prompt });
        this.columns[col.name] = col;
        return col;
    }

    addView(view: EntityView) {
        this.views[view.name] = view;
        return view;
    }

    addProc(proc: EntityProc) {
        this.procs[proc.name] = proc;
        return proc;
    }

    addLookup(lookup: EntityLookup) {
        this.lookups[lookup.name] = lookup;
        return lookup;
    }

    addServerAction(action: EntityServerAction) {
        this.serverActions[action.name] = action;
        return action;
    }

    addClientAction(action: EntityClientAction) {
        this.clientActions[action.name] = action;
        return action;
    }


    static clone(entity_def: EntityDefinition) {
        const columns: Record<string, EntityColumn<Value>> = {};
        for (const colname in entity_def.columns) {
            const source = entity_def.columns[colname];
            columns[colname] = EntityColumn.clone(source);
        }
        const processes: Record<string, EntityProc> = {};
        for (const proc in entity_def.procs) {
            const source = entity_def.procs[proc];
            processes[proc] = { name: source.name };
        }
        const views: Record<string, EntityView> = {};
        for (const view in entity_def.views) {
            const source = entity_def.views[view];
            views[view] = { ...source };
        }
        const lookups: Record<string, EntityLookup> = {};
        for (const lkp in entity_def.lookups) {
            const source = entity_def.lookups[lkp];
            lookups[lkp] = { ...source };
        }
        const serverActions: Record<string, EntityServerAction> = {};
        for (const act in entity_def.serverActions) {
            const source = entity_def.serverActions[act];
            serverActions[act] = { ...source };
        }
        const clientActions: Record<string, EntityClientAction> = {};
        for (const act in entity_def.clientActions) {
            const source = entity_def.clientActions[act];
            clientActions[act] = source;
        }
        const new_def = new EntityDefinition(entity_def.name);
        new_def.columns = columns;
        new_def.procs = processes;
        new_def.views = views;
        new_def.lookups = lookups;
        new_def.serverActions = serverActions;
        new_def.clientActions = clientActions;
        new_def.importColumns = entity_def.importColumns ? [...entity_def.importColumns] : null;
        return new_def;
    };

}