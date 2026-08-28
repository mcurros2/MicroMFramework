import { Entity } from "./Entity";

export interface EntityClientActionImportDataOnClickProps {
    destinationEntity: Entity<any>,
    entityProcName?: string,
    excludedImportDestinations?: string[],
}
