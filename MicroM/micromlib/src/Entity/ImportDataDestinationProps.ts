import type { ImportFileStepCustomizationProps } from "../DataDictionary/ImportEntityData/ImportFileStep";
import { Entity } from "./Entity";
import { EntityDefinition } from "./EntityDefinition";

export interface ImportDataDestinationProps {
    destinationEntity: Entity<EntityDefinition>,
    destinationEntityExportViewName?: string,
    entityProcName?: string,
    excludedImportDestinations?: string[],
    importFileStepProps?: ImportFileStepCustomizationProps,
}
