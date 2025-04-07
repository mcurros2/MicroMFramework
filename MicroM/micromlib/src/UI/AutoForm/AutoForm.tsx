import { Stack } from "@mantine/core";
import { Entity, EntityDefinition } from "../../Entity";
import { FormOptions } from "../Core";
import { EntityForm, useEntityForm } from "../Form";
import { AutoFormFields } from "./AutoFormFields";


export function AutoForm(props: FormOptions<Entity<EntityDefinition>>) {
    const { entity, getDataOnInit, initialFormMode = 'view', onSaved, onCancel } = props;

    const entityForm = useEntityForm(
        {
            entity: entity,
            initialFormMode: initialFormMode,
            validateInputOnBlur: true,
            getDataOnInit: getDataOnInit!,
            onSaved: onSaved,
            onCancel: onCancel,
        }
    );

    return (
        <EntityForm formAPI={entityForm}>
            <Stack>
                <AutoFormFields entity={entity} entityForm={entityForm} />
            </Stack>
        </EntityForm>
    );
}