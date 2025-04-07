import { Button, Card, CardProps, Checkbox, Group, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ComponentType, useRef } from "react";
import { ValuesObject } from "../../client";
import { EntityCardProps } from "../EntityCard";
import { DataViewDefaultProps } from "./DataView";


export interface DataViewCardContainerProps extends EntityCardProps<ValuesObject> {
    EntityCard?: ComponentType<EntityCardProps<ValuesObject>>,
    CardProps?: Omit<CardProps, 'children'>
}

export const DataViewCardContainerDefaultProps: Partial<DataViewCardContainerProps> = {
    CardProps: { shadow: "sm", withBorder: true }
}

export function DataViewCardContainer(props: DataViewCardContainerProps) {
    const {
        recordIndex, selected, handleDeleteClick, handleDeselectRecord, handleEditClick, handleSelectRecord, handleViewClick,
        enableDelete, enableEdit, enableView, entity, record, toggleSelectable, EntityCard, CardProps, refreshView, handleCardClick,
        cardHrefRootURL, cardHrefTarget
    } = useComponentDefaultProps('DataViewCardContainer', DataViewCardContainerDefaultProps, props);

    const theme = useMantineTheme();

    const editElement = useRef<HTMLButtonElement>(null);
    const deleteElement = useRef<HTMLButtonElement>(null);
    const viewElement = useRef<HTMLButtonElement>(null);

    return (
        <Card key={`${entity.def.name}-${recordIndex}`} bg={theme.colorScheme === 'dark' ? theme.colors.dark[9] : undefined} {...CardProps}>
            {toggleSelectable && handleDeselectRecord && handleSelectRecord &&
                <Checkbox key={`chk-${entity.def.name}-${recordIndex}`} size="xs" variant="light" mb="xs" checked={selected} onClick={() => { selected ? handleDeselectRecord(recordIndex) : handleSelectRecord(recordIndex) }} />
            }
            {EntityCard &&
                <EntityCard
                    key={`card-${entity.def.name}-${recordIndex}`}
                    record={record}
                    recordIndex={recordIndex}
                    entity={entity}
                    enableDelete={enableDelete}
                    enableEdit={enableEdit}
                    enableView={enableView}
                    handleSelectRecord={handleSelectRecord}
                    handleDeselectRecord={handleDeselectRecord}
                    handleDeleteClick={handleDeleteClick}
                    handleEditClick={handleEditClick}
                    handleViewClick={handleViewClick}
                    handleCardClick={handleCardClick}
                    selected={selected}
                    toggleSelectable={toggleSelectable}
                    refreshView={refreshView}
                    cardHrefRootURL={cardHrefRootURL}
                    cardHrefTarget={cardHrefTarget}
                />

            }
            {(enableDelete || enableEdit || enableView) &&
                <Card.Section mt="xs" p="xs" withBorder>
                    <Group position="right">
                        {enableView && !enableEdit && handleViewClick &&
                            <Button size="xs" ref={viewElement} onClick={async () => await handleViewClick(record.keys, viewElement.current as HTMLElement)}>{DataViewDefaultProps.labels?.viewLabel}</Button>
                        }
                        {enableEdit && handleEditClick &&
                            <Button size="xs" ref={editElement} onClick={async () => await handleEditClick(record.keys, editElement.current as HTMLElement)}>{DataViewDefaultProps.labels?.editLabel}</Button>
                        }
                        {enableDelete && handleDeleteClick &&
                            <Button size="xs" ref={deleteElement} color="red" onClick={async () => await handleDeleteClick(record.keys, deleteElement.current as HTMLElement)}>{DataViewDefaultProps.labels?.deleteLabel}</Button>
                        }
                    </Group>
                </Card.Section>
            }
        </Card>
    )
}