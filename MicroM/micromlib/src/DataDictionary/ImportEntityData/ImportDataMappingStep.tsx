import { Alert, Stack, useComponentDefaultProps } from "@mantine/core";
import { IconAlertTriangle } from "@tabler/icons-react";
import { ImportDataMappingAPI, ImportDataMappingEditor } from "../../UI/ImportData";

export interface ImportDataMappingStepProps {
    mappingAPI: ImportDataMappingAPI,
    destinations: readonly string[],
    validationError?: string,
}

export const ImportDataMappingStepDefaultProps: Partial<ImportDataMappingStepProps> = {};

export function ImportDataMappingStep(props: ImportDataMappingStepProps) {
    const { mappingAPI, destinations, validationError } = useComponentDefaultProps(
        'ImportDataMappingStep',
        ImportDataMappingStepDefaultProps,
        props
    );

    return (
        <Stack spacing="sm">
            {validationError &&
                <Alert color="yellow" icon={<IconAlertTriangle size="1rem" />}>{validationError}</Alert>
            }
            <ImportDataMappingEditor mappingAPI={mappingAPI} destinations={destinations} />
        </Stack>
    );
}
