import { Entity } from "./Entity";

export interface ImportDataDestinationProps {
    destinationEntity: Entity<any>,
    entityProcName?: string,
    excludedImportDestinations?: string[],
}
