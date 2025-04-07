import { DBStatusResult, DataResult, ImpDataResult, MicroMClient, ValuesObject } from "../client";
import * as cf from "./ColumnsFunctions";
import { EntityColumnFlags } from "./EntityColumn.types";
import { ColumnsObject } from "./EntityColumnCollection.types";
import { EntityError } from "./EntityError";
import { EntityProc } from "./EntityProc";
import { EntityServerAction } from "./EntityServerAction";
import { EntityView } from "./EntityView";

export class EntityAPI {

    client;
    #name;
    #columns;

    constructor(client: MicroMClient, name: string, columns: ColumnsObject) {
        if (!(client instanceof MicroMClient)) throw new Error('Invalid API client.');
        this.client = client;
        this.#columns = columns;
        this.#name = name;
    }

    /**
     * Get the data for the current row and fill the columns values with it.
     */
    async getData(abort_signal: AbortSignal | null = null): Promise<boolean> {

        const values = cf.getValues(this.#columns, { flags: EntityColumnFlags.get, ignoreDefaults: false });

        const result = await this.client.get(this.#name, null, values, abort_signal);

        if (result === null) return false;

        cf.setValues(this.#columns, result, undefined, undefined, undefined, true);

        return true;
    }

    /**
     * Add the data for the current row and fill the key columns values with the returned auto-numeric (if any).
     */
    async addData(abort_signal: AbortSignal | null = null, recordsSelection?: ValuesObject[], ignoreRecordExists?: boolean): Promise<DBStatusResult> {

        const values = cf.getValues(this.#columns, { flags: EntityColumnFlags.add, ignoreDefaults: false });
        const result = await this.client.insert(this.#name, null, values, recordsSelection ?? [], abort_signal);

        if (result.Failed) {
            if (!ignoreRecordExists) {
                throw { Errors: result.Results } as EntityError;
            }
            else {
                // check to see in results if all errors are record exists (status 4). If not, throw the error.
                if (result.Results.some(r => (r.Status !== 4 && r.Status !== 0 && r.Status !== 15))) {
                    throw { Errors: result.Results } as EntityError;
                }
                //ignore the error and return the result.
                result.Failed = false;
            }
        }
        if (result.AutonumReturned) {
            if (result.Results[0].Status !== 15) throw { Errors: result.Results, message: "Unexpected Autonum Status" } as EntityError;
            const autoNumCol = cf.getColumns(this.#columns, { flags: EntityColumnFlags.autoNum, ignoreDefaults: false })[0];
            autoNumCol.value = result.Results[0].Message;
        }
        return result;
    }

    /**
     * Edits the data for the current row.
     */
    async editData(abort_signal: AbortSignal | null = null, recordsSelection?: ValuesObject[]): Promise<DBStatusResult> {

        const values = cf.getValues(this.#columns, { flags: EntityColumnFlags.edit, ignoreDefaults: false });
        const result = await this.client.update(this.#name, null, values, recordsSelection ?? [], abort_signal);
        if (result.Failed) throw { Errors: result.Results } as EntityError;

        return result;
    }

    /**
     * Deletes the data for the current row.
     */
    async deleteData(recordsSelection?: ValuesObject[], abort_signal: AbortSignal | null = null, parentKeys: ValuesObject | null = null): Promise<DBStatusResult> {
        let result;
        if (!recordsSelection) {
            const values = cf.getValues(this.#columns, { flags: EntityColumnFlags.delete, ignoreDefaults: false });
            result = await this.client.delete(this.#name, parentKeys, values, [], abort_signal);

        }
        else {
            result = await this.client.delete(this.#name, parentKeys, {}, recordsSelection, abort_signal);
        }

        if (result.Failed) throw { Errors: result.Results } as EntityError;
        return result;
    }

    /**
     * Lookup the description for an entity.
     */
    async lookupData(abort_signal: AbortSignal | null = null): Promise<string> {
        const values = cf.getValues(this.#columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false });
        const result = await this.client.lookup(this.#name, null, values, null, abort_signal);
        return result.Description;
    }

    /**
     * Import data
     */
    async importData(abort_signal: AbortSignal | null = null, import_procname: string | null = null, parentKeys: ValuesObject | null = null, fileprocess_id: string): Promise<ImpDataResult> {
        const values = { c_fileprocess_id: fileprocess_id };
        const result = await this.client.import(this.#name, parentKeys, values, import_procname, abort_signal);
        return result;
    }

    async executeView(view: EntityView, view_parms?: ValuesObject, limit?: string | null, search?: string[] | null, abort_signal: AbortSignal | null = null): Promise<DataResult[]> {

        if (search) search = search.map(s => (s.indexOf('%') >= 0 ? s : `%${s}%`));
        if (!limit) limit = "";

        cf.setValues(this.#columns, view_parms, null, true);
        let values = cf.getValues(this.#columns, null);

        values['@row_limit'] = limit;
        values.like = search ? search : null;

        if (view_parms) {
            values = {
                ...view_parms,
                ...values
            };
        }

        const result = await this.client.view(this.#name, null, values, view.name, abort_signal);

        return result;
    }

    async executeProc(proc: EntityProc, proc_parms?: ValuesObject, abort_signal: AbortSignal | null = null, recordsSelection?: ValuesObject[]): Promise<DataResult[]> {

        cf.setValues(this.#columns, proc_parms, null, true);
        const values = { ...proc_parms, ...cf.getValues(this.#columns, null) };

        const result = await this.client.proc(this.#name, null, values, recordsSelection ?? [], proc.name, abort_signal);

        return result;
    }

    async executeProcess(proc: EntityProc, proc_parms?: ValuesObject, abort_signal: AbortSignal | null = null, recordsSelection?: ValuesObject[]): Promise<DBStatusResult> {

        cf.setValues(this.#columns, proc_parms, null, true);
        const values = { ...proc_parms, ...cf.getValues(this.#columns, null) };

        const result = await this.client.process(this.#name, null, values, recordsSelection ?? [], proc.name, abort_signal);

        return result;
    }

    async executeServerAction<TReturn extends Record<string, unknown>>(action: EntityServerAction, action_parms?: ValuesObject, abort_signal: AbortSignal | null = null): Promise<TReturn> {

        cf.setValues(this.#columns, action_parms);
        const values = cf.getValues(this.#columns, null);

        const result = await this.client.action<TReturn>(this.#name, null, values, action.name, abort_signal);

        return result;
    }


}