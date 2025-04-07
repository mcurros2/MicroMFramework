import { Modal, Skeleton } from "@mantine/core";
import { ComponentType, useEffect, useState } from "react";
import { Entity, EntityDefinition } from "../../Entity";


export interface ModalFormOptions {
    entity: Entity<EntityDefinition>,
    onSaved: (entity: Entity<EntityDefinition>) => void,
    onCancel: (entity: Entity<EntityDefinition>) => void,
}

export function ModalForm({ entity, onSaved, onCancel }: ModalFormOptions) {
    const [opened, setOpened] = useState(true);
    const [DynamicForm, setDynamicForm] = useState<ComponentType<any> | null>(null);

    useEffect(() => {
        let mounted = true;
        if (entity.Form !== null && entity.Form !== 'AutoForm' && entity.Form !== 'AutoFiltersForm') {
            entity.Form.then(Form => {
                if (mounted) {
                    setDynamicForm(() => Form);
                }
            })
        }
        else {
            import('../AutoForm/AutoForm').then(module => {
                if (mounted) {
                    setDynamicForm(() => module.AutoForm);
                }
            })

        }

        return () => {
            mounted = false;
        };
    }, [entity.Form]);

    return (
        <Modal opened={opened} onClose={() => null}>
            {!DynamicForm && <Skeleton />}
            {DynamicForm &&
                <DynamicForm
                    entity={entity}
                    onSaved={
                        () => {
                            if (onSaved) onSaved(entity);
                            setOpened(false);
                        }
                    }
                    onCancel={
                        () => {
                            if (onCancel) onCancel(entity);
                            setOpened(false);
                        }
                    }
                />
            }
        </Modal>
    );

}