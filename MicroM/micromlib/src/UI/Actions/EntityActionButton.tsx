import { Button, ButtonProps } from "@mantine/core";
import { ValuesObject } from "../../client";
import { useModal } from "../Core";
import { Entity, EntityClientAction, EntityDefinition } from "../../Entity";

export interface EntityActionButtonProps extends Omit<ButtonProps, 'leftIcon'> {
    entity: Entity<EntityDefinition>,
    action: EntityClientAction,
    selectedKeys?: ValuesObject[],
    label?: string,
    onClose?: (result?: boolean) => Promise<boolean>
}

//const TestAction: EntityClientAction = {
//    title: 'test',
//    label: 'test',
//    name: 'test',
//    onClick: async ({ entity, modal, modalProps, selectedKeys }) => {
//        const editEntity = Entity.clone(entity);
//        setValues(editEntity.def.columns, selectedKeys?.[0], null, true);

//        await modal?.open({
//            content: <></>,
//            modalProps: modalProps ?? {}
//        })
//    }
//}

export function EntityActionButton(props: EntityActionButtonProps) {
    const modal = useModal();
    const { entity, action, selectedKeys, label, onClose, ...rest } = props;


    return (
        <Button leftIcon={action.icon} onClick={async () => {
            await action.onClick({
                entity, modal, selectedKeys, onClose
            });
        }}
            {...rest}
        >
            {label || action.label}
        </Button>
    )
}