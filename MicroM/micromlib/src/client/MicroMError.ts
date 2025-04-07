import { FormMode } from "../UI";
import { DBStatus, DBStatusCodes } from "./client.types";

export interface MicroMError extends Error {
    status: number,
    message: string,
    statusMessage?: string;
    url?: string;
}

export const toMicroMError = (e: any) => {
    return {
        status: e.status,
        statusMessage: e.statusMessage ?? e.cause,
        message: e.message,
        url: e.url
    } as MicroMError
}

export const toDBStatusMicroMError = (dbstat: DBStatus[], form_mode: FormMode = 'edit') => {
    const result = dbstat.map(
        stat => (stat.Status == DBStatusCodes.RECORD_CHANGED) ?
            (form_mode == 'add') ? `The record you are trying to add already exists in the database` : `The record you are trying to update has been updated by another user ${stat.Message ?? ''}`
            : `${stat.Message ?? ''}`
    ).join('\n');
    return {
        status: dbstat[0].Status,
        statusMessage: result
    } as MicroMError
}