import { Entity, EntityClientAction, EntityDefinition } from "../../Entity";
import { ValuesObject } from "../../client";
import { DataViewRecord } from "../DataView/DataView.types";

export interface EntityCardProps<T extends ValuesObject> {
    entity: Entity<EntityDefinition>,
    recordIndex: number,
    record: DataViewRecord<T>,
    selected?: boolean,
    toggleSelectable?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
    handleEditClick?: (keys: ValuesObject, element: HTMLElement) => void,
    handleDeleteClick?: (keys: ValuesObject, element: HTMLElement) => void,
    handleViewClick?: (keys: ValuesObject, element: HTMLElement) => void,
    handleSelectRecord?: (record_index: number) => void,
    handleDeselectRecord?: (record_index: number) => void,
    handleExecuteAction?: (action: EntityClientAction, recordIndex?: number, element?: HTMLElement) => Promise<boolean | undefined>,
    handleCardClick?: (data: T) => void,
    refreshView?: () => void,
    cardHrefRootURL?: string
    cardHrefTarget?: string
}