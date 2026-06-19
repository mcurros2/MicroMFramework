import { Accordion, Card, Stack, Text, ThemeIcon, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode, useRef } from "react";
import { MicroMClient } from "../../client";
import { Entity, EntityDefinition } from "../../Entity";
import { FormMode } from "../Core";
import { DataGrid } from "./DataGrid";
import { DataGridProps } from "./DataGrid.types";

export interface DataGridPageProps extends Omit<DataGridProps, 'entity' | 'title'> {
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    client: MicroMClient,
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
    showTitle?: boolean
}

export const DataGridPageDefaultProps: Partial<DataGridPageProps> = {
    showTitle: true
}

export function DataGridPage(props: DataGridPageProps) {
    const {
        title, icon, formMode, helpText, showTitle, client, enableAdd, enableDelete, enableEdit,
        entityConstructor, gridHeight, ...others
    } = useComponentDefaultProps('DataGridPage', props, DataGridPageDefaultProps);

    const theme = useMantineTheme();
    //const gridRef = useRef(null);

    const entity = useRef<Entity<EntityDefinition>>(entityConstructor(client));

    const EntityIcon = entity.current.Icon;

    return (
        <Stack style={{ height: gridHeight === 'flex-grow' ? '100%' : undefined }}>
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
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true} style={{ flexGrow: gridHeight === 'flex-grow' ? 1 : undefined }}>
                <DataGrid
                    {...others}
                    entity={entity.current}
                    enableAdd={formMode === 'view' ? false : enableAdd}
                    enableEdit={formMode === 'view' ? false : enableEdit}
                    enableDelete={formMode === 'view' ? false : enableDelete}
                    formMode={formMode}
                    gridHeight={gridHeight}
                //ref={gridRef}
                />
            </Card>
        </Stack>
    )
}