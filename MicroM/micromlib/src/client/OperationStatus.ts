import { DataResult, DBStatusResult, ValuesObject } from "./client.types";
import { MicroMError } from "./MicroMError";
import { MicroMToken } from "./MicroMToken";


export type DataOperationType = "add" | "edit" | "delete" | "get" | "lookup" | "view" | "action" | "login" | "refresh" | "proc" | "import" | "other";

export interface OperationStatus<T> {
    loading?: boolean;
    error?: MicroMError;
    operationType?: DataOperationType,
    data?: T;
}

export type StatusCompletedHandler<T extends MicroMToken | DBStatusResult | DataResult | ValuesObject | null> = (status: OperationStatus<T>) => void;

