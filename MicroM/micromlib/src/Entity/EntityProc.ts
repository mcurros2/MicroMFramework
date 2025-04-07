import { Value } from "../client";

export interface EntityProc {
    name: string,
    parms?: Record<string, Value>,
}
