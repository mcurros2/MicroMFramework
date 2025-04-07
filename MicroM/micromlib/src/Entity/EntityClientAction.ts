import { ReactNode } from "react";
import { ModalContextType } from "../UI";
import { ValuesObject } from "../client";
import { Entity } from "./Entity";

export interface EntityClientActionOnClickProps {
    entity: Entity<any>,
    modal?: ModalContextType,
    selectedKeys?: ValuesObject[],
    element?: HTMLElement,
    onClose?: (result?: boolean) => Promise<boolean>
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
