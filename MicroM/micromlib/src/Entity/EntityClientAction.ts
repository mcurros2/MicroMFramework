import { ReactNode } from "react";
import { DBStatusResult, OperationStatus, ValuesObject } from "../client";
import { ModalContextType } from "../UI";
import type { FormMode } from "../UI/Core";
import { Entity } from "./Entity";

export interface EntityClientActionOnClickProps {
    entity: Entity<any>,
    modal?: ModalContextType,
    selectedKeys?: ValuesObject[],
    element?: HTMLElement,
    onClose?: (result?: boolean, status?: OperationStatus<DBStatusResult>) => Promise<boolean>,
}

export interface EntityClientAction {
    name: string,
    title: ReactNode,
    label: ReactNode,
    icon?: ReactNode,
    refreshOnClose?: boolean,
    dontRequireSelection?: boolean,
    minSelectedRecords?: number,
    maxSelectedRecords?: number,
    views?: string[],
    showActionInViewMode?: boolean,
    onClick: (props: EntityClientActionOnClickProps) => Promise<boolean>,
}

export interface EntityFormClientActionOnClickProps extends EntityClientActionOnClickProps {
    silent: boolean,
}

export interface EntityFormClientAction extends Omit<EntityClientAction, 'onClick'> {
    onClick: (props: EntityFormClientActionOnClickProps) => Promise<boolean>,
}

export interface EntityFormActions {
    OK?: EntityFormClientAction,
    Cancel?: EntityFormClientAction,
}

export type EntityFormActionOverrides = Partial<Record<FormMode | 'Allways', EntityFormActions>>;
