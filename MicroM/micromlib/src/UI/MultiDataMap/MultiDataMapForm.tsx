import { Accordion, Card, Text, ThemeIcon, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode, useMemo } from "react";
import { FormMode } from "../Core";
import { MultiDataMap, MultiDataMapProps, MultiDataMapViewProps } from "./MultiDataMap";

export interface MultiDataMapFormProps extends MultiDataMapProps {
    customTitle?: ReactNode,
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    showTitle?: boolean
}

export const MultiDataMapFormDefaultProps: Partial<MultiDataMapFormProps> = {
    showTitle: true
}

export function MultiDataMapForm(props: MultiDataMapFormProps) {
    const {
        showTitle, formMode, title, icon, helpText, customTitle, dataMapView1, dataMapView2, dataMapView3, dataMapView4, dataMapView5, ...others
    } = useComponentDefaultProps('MultiDataMapForm', props, MultiDataMapFormDefaultProps);

    const theme = useMantineTheme();


    const { entity } = dataMapView1;

    const EntityIcon = dataMapView1.entity?.Icon;
    const IconNode = (icon) ? <ThemeIcon>{icon}</ThemeIcon> : (EntityIcon) ? <ThemeIcon><EntityIcon size="1rem" /></ThemeIcon> : undefined;

    const dataMapViews = useMemo(() => {
        const updateDataMapView = (dataMapView?: MultiDataMapViewProps) => {
            if (!dataMapView) return dataMapView;
            const updatedDataMapView = {
                ...dataMapView,
                enableAdd: formMode === 'view' ? false : dataMapView.enableAdd,
                enableEdit: formMode === 'view' ? false : dataMapView.enableEdit,
                enableDelete: formMode === 'view' ? false : dataMapView.enableDelete,
                enableView: formMode === 'view' ? true : dataMapView.enableView
            };

            // Only return a new object if any of the properties changed
            if (
                updatedDataMapView.enableAdd !== dataMapView.enableAdd ||
                updatedDataMapView.enableEdit !== dataMapView.enableEdit ||
                updatedDataMapView.enableDelete !== dataMapView.enableDelete ||
                updatedDataMapView.enableView !== dataMapView.enableView
            ) {
                return updatedDataMapView;
            }

            return dataMapView;
        };

        return {
            newDataMapView1: updateDataMapView(dataMapView1),
            newDataMapView2: updateDataMapView(dataMapView2),
            newDataMapView3: updateDataMapView(dataMapView3),
            newDataMapView4: updateDataMapView(dataMapView4),
            newDataMapView5: updateDataMapView(dataMapView5),
        };

    }, [dataMapView1, dataMapView2, dataMapView3, dataMapView4, dataMapView5, formMode]);

    const { newDataMapView1, newDataMapView2, newDataMapView3, newDataMapView4, newDataMapView5 } = dataMapViews;

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
            <MultiDataMap
                dataMapView1={newDataMapView1!}
                dataMapView2={newDataMapView2}
                dataMapView3={newDataMapView3}
                dataMapView4={newDataMapView4}
                dataMapView5={newDataMapView5}
                formMode={formMode}
                {...others}
            />
        </Card>
    );
}