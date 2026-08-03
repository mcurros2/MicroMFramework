import { ValuesObject } from "../../client";
import { areValuesObjectsEqual, copyValuesObject, Entity, EntityDefinition, EntityProc } from "../../Entity";

export interface ExecuteRequest {
    values?: ValuesObject;
}

export interface ExecuteProcRequest extends ExecuteRequest {
    entity: Entity<EntityDefinition>;
    proc: EntityProc;
}

export type ExecuteInFlight<TRequest extends ExecuteRequest> = TRequest & {
    controller: AbortController;
    token: symbol;
};

export function copyRequestValues(values?: ValuesObject): ValuesObject | undefined {
    return values === undefined ? undefined : copyValuesObject(values);
}

export function areExecuteProcRequestsEqual(
    a: ExecuteProcRequest | undefined,
    b: ExecuteProcRequest
): boolean {
    return a?.entity === b.entity &&
        a.proc === b.proc &&
        areValuesObjectsEqual(a.values, b.values);
}
