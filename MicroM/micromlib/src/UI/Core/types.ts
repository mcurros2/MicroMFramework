import { DBStatusResult, OperationStatus } from "../../client";
import { Entity, EntityDefinition } from "../../Entity";
import { EntityFormProps } from "../Form";

export type FormMode = 'add' | 'edit' | 'view';

export interface KeyStringIndexer {
    [key: string]: any
}

export interface NumberIndexer {
    [key: number]: any
}

export interface FormOptions<T extends Entity<EntityDefinition>> extends Omit<EntityFormProps, 'formAPI' | 'children'> {
    entity: T,
    initialFormMode: FormMode,
    getDataOnInit?: boolean,
    onSaved?: (status: OperationStatus<DBStatusResult>) => void,
    onCancel?: () => void
}

export type useStateReturnType<T> = [T, React.Dispatch<React.SetStateAction<T>>];

export type ActionIconVariant = 'transparent' | 'subtle' | 'default' | 'outline' | 'filled' | 'light';

export type ButtonVariant = 'gradient' | 'subtle' | 'default' | 'outline' | 'filled' | 'light' | 'white';

export type latLng = { lat: number, lng: number };
