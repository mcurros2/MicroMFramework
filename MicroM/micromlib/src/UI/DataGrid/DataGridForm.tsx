import { Accordion, Card, Text, ThemeIcon, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode } from "react";
import { FormMode } from "../Core";
import { DataGrid } from "./DataGrid";
import { DataGridProps } from "./DataGrid.types";

export interface DataGridFormProps extends DataGridProps {
    customTitle?: ReactNode,
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    showTitle?: boolean
}

export const DataGridFormDefaultProps: Partial<DataGridFormProps> = {
    showTitle: true
}

export function DataGridForm(props: DataGridFormProps) {
    const {
        title, icon, helpText, formMode, showTitle, entity,
        enableAdd, enableDelete, enableEdit, customTitle,
        enableView, ...gridProps
    } = useComponentDefaultProps('DataGridForm', props, DataGridFormDefaultProps);

    const theme = useMantineTheme();
    //const gridRef = useRef(null);

    const EntityIcon = entity?.Icon;

    const IconNode = (icon) ? <ThemeIcon>{icon}</ThemeIcon> : (EntityIcon) ? <ThemeIcon><EntityIcon size="1rem" /></ThemeIcon> : undefined;

    return (
        <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
            {showTitle && customTitle &&
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    {customTitle}
                </Card.Section>
            }
            {showTitle && !customTitle &&
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    <Accordion
                        variant="filled"
                        chevron={<IconHelp size="1rem" />}
                        styles={{ item: { '&[data-active]': { backgroundColor: "unset" } }, label: { paddingBottom: 0, paddingTop: 0 }, control: { padding: 0 } }}
                    >
                        <Accordion.Item value={entity?.name ?? 'dgfEntity'} >
                            <Accordion.Control icon={IconNode}>
                                <Text size="sm">{title ?? entity?.Title}</Text>
                            </Accordion.Control>
                            <Accordion.Panel>
                                <Text size="xs">
                                    {helpText ?? entity?.HelpText}
                                </Text>
                            </Accordion.Panel>
                        </Accordion.Item>
                    </Accordion>
                </Card.Section>
            }
            <DataGrid
                entity={entity}
                enableAdd={formMode === 'view' ? false : enableAdd}
                enableEdit={formMode === 'view' ? false : enableEdit}
                enableDelete={formMode === 'view' ? false : enableDelete}
                formMode={formMode}
                {...gridProps}
                //ref={gridRef}
            />
        </Card>
    )
}