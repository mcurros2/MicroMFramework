import { Stack } from "@mantine/core";
import { Entity, EntityDefinition } from "../../Entity";
import { AutoFormFields } from "../AutoForm";
import { FormOptions } from "../Core";
import { useViewFiltersForm } from "../DataGrid/useViewFiltersForm";
import { EntityForm } from "../Form";


export const ApplyFiltersDefaultLabel = 'Apply Filters';

export function AutoFiltersForm(props: FormOptions<Entity<EntityDefinition>>) {
    const { entity, getDataOnInit, initialFormMode = 'edit', onSaved, onCancel } = props;

    const formAPI = useViewFiltersForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel });


    return (
        <EntityForm formAPI={formAPI} OKText={ApplyFiltersDefaultLabel}>
            <Stack>
                <AutoFormFields entity={entity} entityForm={formAPI} />
            </Stack>
        </EntityForm>
    );
}