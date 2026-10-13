import { ReactNode } from "react";
import { Entity, EntityColumnFlags, EntityDefinition, SYSTEM_COLUMNS_NAMES } from "../../Entity";
import { CheckboxField, DateInputField, NumberField } from "../Core";
import { resolveFieldReadOnly } from "../Core/fieldUIHelpers";
import { TextAreaField } from "../Core/TextAreaField";
import { TextField } from "../Core/TextField";
import { UseEntityFormReturnType } from "../Form";


export interface AutoFormFieldsProps<T extends Entity<EntityDefinition>> {
    entity: T,
    entityForm: UseEntityFormReturnType
}

export function AutoFormFields(props: AutoFormFieldsProps<Entity<EntityDefinition>>) {
    const { entity, entityForm } = props;

    let first_focus: boolean | undefined = true;

    return Object.entries(entity.def.columns).map(([key, column]) => {
        if (!SYSTEM_COLUMNS_NAMES.includes(key) && !column.excludeInAutoForm) {
            let element: ReactNode = undefined;
            const fieldReadOnly = resolveFieldReadOnly(column, entityForm.formMode);
            const fieldAutoFocus = !fieldReadOnly ? first_focus : undefined;
            switch (column.type) {
                case 'nvarchar':
                case 'varchar':
                case 'nchar':
                case 'char':
                    if (entityForm.formMode === 'add' && column.hasFlag(EntityColumnFlags.autoNum)) break;
                    if (column.length > 0 && column.length <= 255) {
                        element = <TextField
                            column={column}
                            entityForm={entityForm}
                            readOnly={column.hasFlag(EntityColumnFlags.autoNum) || (entityForm.formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk))}
                            autoFocus={fieldAutoFocus}
                            key={`${entity.name}${column.name}`}
                            description={column.description}
                            placeholder={column.placeholder}
                            maw={column.length === 20 ? "20rem" : undefined}
                        />
                    }
                    else {
                        element = <TextAreaField
                            column={column}
                            entityForm={entityForm}
                            label={column.prompt}
                            maxRows={4}
                            minRows={4}
                            autoFocus={fieldAutoFocus}
                            key={`${entity.name}${column.name}`}
                            description={column.description}
                            placeholder={column.placeholder}
                        />
                    }
                    break;
                case 'date':
                case 'datetime':
                case 'datetime2':
                    element = <DateInputField
                        column={column}
                        entityForm={entityForm}
                        label={column.prompt}
                        autoFocus={fieldAutoFocus}
                        key={`${entity.name}${column.name}`}
                        description={column.description}
                        placeholder={column.placeholder}
                    />
                    break;
                case 'bigint':
                case 'int':
                case 'tinyint':
                case 'smallint':
                    element = <NumberField
                        column={column}
                        entityForm={entityForm}
                        readOnly={column.hasFlag(EntityColumnFlags.autoNum) || undefined}
                        autoFocus={fieldAutoFocus}
                        key={`${entity.name}${column.name}`}
                        description={column.description}
                        placeholder={column.placeholder}
                        maw={"20rem"}
                    />
                    break;
                case 'decimal':
                case 'float':
                case 'money':
                case 'real':
                    element = <NumberField
                        column={column}
                        entityForm={entityForm}
                        readOnly={column.hasFlag(EntityColumnFlags.autoNum) || undefined}
                        autoFocus={fieldAutoFocus}
                        key={`${entity.name}${column.name}`}
                        description={column.description}
                        placeholder={column.placeholder}
                        maw={"20rem"}
                        precision={column.scale}
                    />
                    break;
                case 'bit':
                    element = <CheckboxField
                        column={column}
                        entityForm={entityForm}
                        label={column.prompt}
                        autoFocus={fieldAutoFocus}
                        key={`${entity.name}${column.name}`}
                        description={column.description}
                    />
                    break;
            }
            if (element !== undefined && fieldAutoFocus) first_focus = undefined;
            return element;
        }
        return undefined
    });
}