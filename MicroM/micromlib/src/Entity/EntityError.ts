import { DBStatus } from "../client";

export interface EntityError extends Error {
    Errors: DBStatus[]
}
