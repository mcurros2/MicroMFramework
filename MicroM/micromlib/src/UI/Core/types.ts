import { ReactNode } from "react";
import { DBStatusResult, OperationStatus, ValuesObject } from "../../client";
import { Entity, EntityDefinition } from "../../Entity";
import { EntityFormProps } from "../Form";
import { NavigationProtectionMode } from "../Router/NavigationGuards";

export type FormMode = 'add' | 'edit' | 'view';

export interface KeyStringIndexer {
    [key: string]: any
}

export interface NumberIndexer {
    [key: number]: any
}

export type ValidateFormResult =
    | { success: true, warning?: never, error?: never }
    | { warning: ReactNode, success?: never, error?: never }
    | { error: ReactNode, success?: never, warning?: never };

export type ValidateFormCallback = (values: ValuesObject) => ValidateFormResult | Promise<ValidateFormResult>;

export interface FormOptions<T extends Entity<EntityDefinition>> extends Omit<EntityFormProps, 'formAPI' | 'children'> {
    entity: T,
    initialFormMode: FormMode,
    getDataOnInit?: boolean,
    onSaved?: (status: OperationStatus<DBStatusResult>) => void,
    onCancel?: () => void,
    navigationProtection?: NavigationProtectionMode,
    validateForm?: ValidateFormCallback,
}

export type useStateReturnType<T> = [T, React.Dispatch<React.SetStateAction<T>>];

export type ActionIconVariant = 'transparent' | 'subtle' | 'default' | 'outline' | 'filled' | 'light';

export type ButtonVariant = 'gradient' | 'subtle' | 'default' | 'outline' | 'filled' | 'light' | 'white';

export type latLng = { lat: number, lng: number };

export const MicroMWidthSizes = {
    xs: "10rem",
    sm: "20rem",
    md: "30rem",
    lg: "40rem",
    xl: "50rem",
    auto: 'auto',
}
