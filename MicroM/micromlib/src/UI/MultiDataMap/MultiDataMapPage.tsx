import { Accordion, Card, Stack, Text, ThemeIcon, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconHelp } from "@tabler/icons-react";
import { ReactNode, useMemo, useRef } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { MicroMClient } from "../../client";
import { FormMode } from "../Core";
import { MultiDataMap, MultiDataMapProps, MultiDataMapViewProps } from "./MultiDataMap";

export interface MultiDataMapPageViewProps extends Omit<MultiDataMapViewProps, 'entity'> {
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
}

export interface MultiDataMapPageProps extends Omit<MultiDataMapProps, 'dataMapView1' | 'dataMapView2' | 'dataMapView3' | 'dataMapView4' | 'dataMapView5'> {
    dataMapView1: MultiDataMapPageViewProps,
    dataMapView2?: MultiDataMapPageViewProps,
    dataMapView3?: MultiDataMapPageViewProps,
    dataMapView4?: MultiDataMapPageViewProps,
    dataMapView5?: MultiDataMapPageViewProps,
    title?: string,
    icon?: ReactNode,
    helpText?: string,
    formMode: FormMode,
    client: MicroMClient,
    showTitle?: boolean
}

export const MultiDataMapPageDefaultProps: Partial<MultiDataMapPageProps> = {
    showTitle: true
}

export function MultiDataMapPage(props: MultiDataMapPageProps) {
    const {
        showTitle, client, formMode, title, icon, helpText, dataMapView1, dataMapView2, dataMapView3, dataMapView4, dataMapView5, ...others
    } = useComponentDefaultProps('MultiDataMapPage', props, MultiDataMapPageDefaultProps);

    const theme = useMantineTheme();

    const entity1 = useRef<Entity<EntityDefinition>>(dataMapView1.entityConstructor(client));
    const entity2 = useRef<Entity<EntityDefinition> | undefined>(dataMapView2?.entityConstructor(client));
    const entity3 = useRef<Entity<EntityDefinition> | undefined>(dataMapView3?.entityConstructor(client));
    const entity4 = useRef<Entity<EntityDefinition> | undefined>(dataMapView4?.entityConstructor(client));
    const entity5 = useRef<Entity<EntityDefinition> | undefined>(dataMapView5?.entityConstructor(client));


    const dataMapViews = useMemo(() => {
        const updateDataMapView = (dataMapView: MultiDataMapViewProps | undefined, entity: Entity<EntityDefinition> | undefined) => {
            if (!dataMapView) return dataMapView;
            const updatedDataMapView: MultiDataMapViewProps = {
                ...dataMapView,
                entity: entity,
                enableAdd: formMode === 'view' ? false : dataMapView.enableAdd,
                enableEdit: formMode === 'view' ? false : dataMapView.enableEdit,
                enableDelete: formMode === 'view' ? false : dataMapView.enableDelete,
                enableView: formMode === 'view' ? true : dataMapView.enableView,
            };

            // Only return a new object if any of the properties changed
            if (
                updatedDataMapView.enableAdd !== dataMapView.enableAdd ||
                updatedDataMapView.enableEdit !== dataMapView.enableEdit ||
                updatedDataMapView.enableDelete !== dataMapView.enableDelete ||
                updatedDataMapView.enableView !== dataMapView.enableView ||
                (updatedDataMapView.entity && !dataMapView.entity)
            ) {
                return updatedDataMapView;
            }

            return dataMapView;
        };

        return {
            newDataMapView1: updateDataMapView(dataMapView1, entity1.current),
            newDataMapView2: updateDataMapView(dataMapView2, entity2.current),
            newDataMapView3: updateDataMapView(dataMapView3, entity3.current),
            newDataMapView4: updateDataMapView(dataMapView4, entity4.current),
            newDataMapView5: updateDataMapView(dataMapView5, entity5.current),
        };

    }, [dataMapView1, dataMapView2, dataMapView3, dataMapView4, dataMapView5, formMode]);

    const { newDataMapView1, newDataMapView2, newDataMapView3, newDataMapView4, newDataMapView5 } = dataMapViews;

    const EntityIcon = entity1.current.Icon;

    return (
        <Stack>
            {showTitle &&
                <Accordion
                    variant="filled"
                    chevron={<IconHelp size="1.5rem" />}
                    styles={{ item: { '&[data-active]': { backgroundColor: "unset" } }, label: { paddingBottom: 0, paddingTop: 0 } }}
                >
                    <Accordion.Item value={entity1.current.name} >
                        <Accordion.Control icon={(icon) ? <ThemeIcon>{icon}</ThemeIcon> : (EntityIcon) ? <ThemeIcon><EntityIcon size="1.3rem" /></ThemeIcon> : undefined}>
                            <Title order={2} >{title ?? entity1.current.Title}</Title>
                        </Accordion.Control>
                        <Accordion.Panel>
                            <Text size="sm">
                                {helpText ?? entity1.current.HelpText}
                            </Text>
                        </Accordion.Panel>
                    </Accordion.Item>
                </Accordion>
            }
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
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
        </Stack>
    );
}