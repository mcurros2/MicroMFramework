import type { ImportFileStepCustomizationProps } from "../DataDictionary/ImportEntityData/ImportFileStep";
import type { StepperFormProps } from "../UI/Form";
import { Entity } from "./Entity";
import { EntityDefinition } from "./EntityDefinition";

export interface ImportDataDestinationProps {
    destinationEntity: Entity<EntityDefinition>,
    destinationEntityExportViewName?: string,
    entityProcName?: string,
    importFileStepProps?: ImportFileStepCustomizationProps,
    stepperProps?: StepperFormProps['stepperProps'],
}
