import { ActionIcon, Box, Button, Group, Menu, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconEye, IconEyeOff, IconSettingsPin } from "@tabler/icons-react";
import { EntityClientAction } from "../../Entity";
import { ActionIconVariant, ButtonVariant, FormMode } from "../Core";
import { DataGridToolbarSizes } from "../DataGrid";
import { getToolbarSizes } from "../DataGrid/ToolBarFunctions";
import { useMultiDataMapGrid } from "./useMultiDataMapGrid";
import { useRef } from "react";

export interface MultiDataMapActionsToolbarProps {
    showMapConfig?: boolean,
    clientActions?: Record<string, EntityClientAction>,
    parentFormMode?: FormMode,
    data1?: ReturnType<typeof useMultiDataMapGrid>,
    data2?: ReturnType<typeof useMultiDataMapGrid>,
    data3?: ReturnType<typeof useMultiDataMapGrid>,
    data4?: ReturnType<typeof useMultiDataMapGrid>,
    data5?: ReturnType<typeof useMultiDataMapGrid>,
    configMenuTitle?: string,
    size?: DataGridToolbarSizes,
    toolbarIconVariant?: ActionIconVariant,
    actionsButtonVariant?: ButtonVariant,
    
}

export const MultiDataMapActionsToolbarDefaultProps: Partial<MultiDataMapActionsToolbarProps> = {
    showMapConfig: true,
    configMenuTitle: 'Map Config',
    size: "sm",
    toolbarIconVariant: "light",
    actionsButtonVariant: "light",
    
}

export function MultiDataMapActionsToolbar(props: MultiDataMapActionsToolbarProps) {
    const {
        showMapConfig, data1, data2, data3, data4, data5, configMenuTitle, size, toolbarIconVariant, parentFormMode,
        actionsButtonVariant
    } = useComponentDefaultProps('MultiDataMapActionsToolbar', MultiDataMapActionsToolbarDefaultProps, props);

    const theme = useMantineTheme();
    const { buttonsSize, actionIconSize, iconsSize } = getToolbarSizes(size!);

    const actionElements = useRef<(HTMLButtonElement | null)[]>([]);

    return (
        <Box>
            <Group>
                {data1?.showOnMap && data1.dgProps.entity && data1.dgProps.showActions && data1.dgProps.entity.def.clientActions &&
                    Object.values(data1.dgProps.entity.def.clientActions).map(
                        (action, index) => {
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (data1.dgProps.viewName && action.views && !action.views.includes(data1.dgProps.viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => data1.dataGridAPI.handleExecuteAction(action, undefined, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
                {data2?.showOnMap && data2.dgProps.entity && data2.dgProps.showActions && data2.dgProps.entity.def.clientActions &&
                    Object.values(data2.dgProps.entity.def.clientActions).map(
                        (action, index) => {
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (data2.dgProps.viewName && action.views && !action.views.includes(data2.dgProps.viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => data2.dataGridAPI.handleExecuteAction(action, undefined, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
                {data3?.showOnMap && data3.dgProps.entity && data3.dgProps.showActions && data3.dgProps.entity.def.clientActions &&
                    Object.values(data3.dgProps.entity.def.clientActions).map(
                        (action, index) => {
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (data3.dgProps.viewName && action.views && !action.views.includes(data3.dgProps.viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => data3.dataGridAPI.handleExecuteAction(action, undefined, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
                {data4?.showOnMap && data4.dgProps.entity && data4.dgProps.showActions && data4.dgProps.entity.def.clientActions &&
                    Object.values(data4.dgProps.entity.def.clientActions).map(
                        (action, index) => {
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (data4.dgProps.viewName && action.views && !action.views.includes(data4.dgProps.viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => data4.dataGridAPI.handleExecuteAction(action, undefined, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
                {data5?.showOnMap && data5.dgProps.entity && data5.dgProps.showActions && data5.dgProps.entity.def.clientActions &&
                    Object.values(data5.dgProps.entity.def.clientActions).map(
                        (action, index) => {
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (data5.dgProps.viewName && action.views && !action.views.includes(data5.dgProps.viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => data5.dataGridAPI.handleExecuteAction(action, undefined, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
                {showMapConfig &&
                    <Menu>
                        <Menu.Target>
                            <ActionIcon
                                title={configMenuTitle}
                                ml="auto"
                                size={actionIconSize}
                                radius="xl"
                                color={theme.primaryColor}
                                variant={toolbarIconVariant} >
                                <IconSettingsPin size={iconsSize} stroke="1.5" />
                            </ActionIcon>
                        </Menu.Target>
                        <Menu.Dropdown>
                            {data1?.dgProps.entity &&
                                <Menu.Item
                                    icon={data1.showOnMap ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                                    onClick={() => data1.setShowOnMap((prev) => prev = !prev)}
                                >
                                    {data1.dgProps.tabLabel}
                                </Menu.Item>
                            }
                            {data2?.dgProps.entity &&
                                <Menu.Item
                                    icon={data2.showOnMap ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                                    onClick={() => data2.setShowOnMap((prev) => prev = !prev)}
                                >
                                    {data2.dgProps.tabLabel}
                                </Menu.Item>
                            }
                            {data3?.dgProps.entity &&
                                <Menu.Item
                                    icon={data3.showOnMap ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                                    onClick={() => data3.setShowOnMap((prev) => prev = !prev)}
                                >
                                    {data3.dgProps.tabLabel}
                                </Menu.Item>
                            }
                            {data4?.dgProps.entity &&
                                <Menu.Item
                                    icon={data4.showOnMap ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                                    onClick={() => data4.setShowOnMap((prev) => prev = !prev)}
                                >
                                    {data4.dgProps.tabLabel}
                                </Menu.Item>
                            }
                            {data5?.dgProps.entity &&
                                <Menu.Item
                                    icon={data5.showOnMap ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                                    onClick={() => data5.setShowOnMap((prev) => prev = !prev)}
                                >
                                    {data5.dgProps.tabLabel}
                                </Menu.Item>
                            }
                        </Menu.Dropdown>
                    </Menu>
                }
            </Group>
        </Box>
    );
}