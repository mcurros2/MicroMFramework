import { Accordion, Card, Stack, Text, ThemeIcon, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode, useMemo, useRef } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { MicroMClient } from "../../client";
import { FormMode } from "../Core";
import { DataGridProps } from "../DataGrid";
import { DataMap, DataMapProps } from "./DataMap";

export interface DataMapPageProps extends Omit<DataMapProps, 'dataGridProps'> {
    dataGridProps: Omit<DataGridProps, 'gridHeight' | 'entity'>,
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    client: MicroMClient,
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
    showTitle?: boolean
}

export const DataMapPageDefaultProps: Partial<DataMapPageProps> = {
    showTitle: true
}


export function DataMapPage(props: DataMapPageProps) {
    const {
        showTitle, entityConstructor, client, formMode, title, icon, helpText, dataGridProps, ...others
    } = useComponentDefaultProps('DataMapPage', props, DataMapPageDefaultProps);

    const theme = useMantineTheme();

    const entity = useRef<Entity<EntityDefinition>>(entityConstructor(client));

    const EntityIcon = entity.current.Icon;

    const dgProps = useMemo<DataGridProps>(() => {
        const enableAdd = formMode === 'view' ? false : dataGridProps.enableAdd;
        const enableEdit = formMode === 'view' ? false : dataGridProps.enableEdit;
        const enableDelete = formMode === 'view' ? false : dataGridProps.enableDelete;
        return {
            ...dataGridProps,
            entity: entity.current,
            enableAdd: enableAdd,
            enableEdit: enableEdit,
            enableDelete: enableDelete,
        }
    }, [dataGridProps, formMode])

    return (
        <Stack>
            {showTitle &&
                <Accordion
                    variant="filled"
                    chevron={<IconHelp size="1.5rem" />}
                    styles={{ item: { '&[data-active]': { backgroundColor: "unset" } }, label: { paddingBottom: 0, paddingTop: 0 } }}
                >
                    <Accordion.Item value={entity.current.name} >
                        <Accordion.Control icon={(icon) ? <ThemeIcon>{icon}</ThemeIcon> : (EntityIcon) ? <ThemeIcon><EntityIcon size="1.3rem" /></ThemeIcon> : undefined}>
                            <Title order={2} >{title ?? entity.current.Title}</Title>
                        </Accordion.Control>
                        <Accordion.Panel>
                            <Text size="sm">
                                {helpText ?? entity.current.HelpText}
                            </Text>
                        </Accordion.Panel>
                    </Accordion.Item>
                </Accordion>
            }
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
                <DataMap
                    dataGridProps={dgProps}
                    {...others}
                />
            </Card>
        </Stack>
    );
}