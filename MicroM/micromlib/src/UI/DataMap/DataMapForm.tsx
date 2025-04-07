import { Accordion, Card, Text, ThemeIcon, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode, useMemo } from "react";
import { FormMode } from "../Core";
import { DataGridProps } from "../DataGrid";
import { DataMap, DataMapProps } from "./DataMap";

export interface DataMapFormProps extends DataMapProps {
    customTitle?: ReactNode,
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    showTitle?: boolean,
}

export const DataMapFormDefaultProps: Partial<DataMapFormProps> = {
    showTitle: true
}

export function DataMapForm(props: DataMapFormProps) {
    const {
        showTitle, formMode, title, icon, helpText, customTitle, dataGridProps, ...others
    } = useComponentDefaultProps('DataMapForm', props, DataMapFormDefaultProps);

    const theme = useMantineTheme();

    const dgProps = useMemo<DataGridProps>(() => {
        const enableAdd = formMode === 'view' ? false : dataGridProps.enableAdd;
        const enableEdit = formMode === 'view' ? false : dataGridProps.enableEdit;
        const enableDelete = formMode === 'view' ? false : dataGridProps.enableDelete;
        return {
            ...dataGridProps,
            enableAdd: enableAdd,
            enableEdit: enableEdit,
            enableDelete: enableDelete,
        }
    }, [dataGridProps, formMode])

    const { entity } = dgProps;

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
                        <Accordion.Item value={entity?.name ?? 'dmfEntity'} >
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
            <DataMap
                dataGridProps={dgProps}
                {...others}
            />
        </Card>
    );
}