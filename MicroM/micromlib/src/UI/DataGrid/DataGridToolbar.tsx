import { Accordion, ActionIcon, Badge, BadgeVariant, Group, MantineNumberSize, MantineSize, Menu, SelectItem, Stack, Text, rem, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { AccordionVariant } from "@mantine/core/lib/Accordion/Accordion.types";
import { IconCloudUpload, IconDownload, IconEyeCog, IconFilter, IconFilterOff, IconPencil, IconReload, IconSquareCheck, IconSquareCheckFilled, IconX } from "@tabler/icons-react";
import { Dispatch, ReactNode, SetStateAction } from "react";
import { ColumnsObject, EntityConstructor } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { ActionIconVariant, SearchFilterInput, ToggleActionIcon } from "../Core";
import { getToolbarSizes } from "./ToolBarFunctions";
import { useDataGridToolbarFilters } from "./useDataGridToolbarFilters";

export type DataGridToolbarSizes = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface DataGridToolbarOptions {
    size?: DataGridToolbarSizes,
    client?: MicroMClient,
    searchPlaceholder?: string,
    hideCheckboxToggle?: boolean,
    onCheckboxToggle?: () => void,
    onSearchTextChange?: (text: string[] | undefined) => void,
    onRefreshClick: (searchText: string[] | undefined) => void,
    onExportClick: () => void,
    autoFocus?: boolean,
    toolbarIconVariant?: ActionIconVariant,
    enableExport?: boolean,
    refreshTooltip?: string,
    exportTooltip?: string,
    selectRowsTooltip?: string,
    searchText: string[] | undefined,
    setSearchText: Dispatch<SetStateAction<string[] | undefined>>,
    searchData: SelectItem[],
    setSearchData: Dispatch<SetStateAction<SelectItem[]>>,

    FiltersEntity?: EntityConstructor,
    filterTooltip?: string,
    parentKeys?: ValuesObject,

    filterValues: ValuesObject | undefined,
    setFilterValues: Dispatch<SetStateAction<ValuesObject | undefined>>,

    filtersDescription: ValuesObject | undefined,
    setFiltersDescription: Dispatch<SetStateAction<ValuesObject | undefined>>,

    clearFiltersTooltip?: string,
    filtersFormSize: MantineNumberSize

    enableImport?: boolean,
    importTooltip?: string,
    onImportClick?: () => void,

    //toolbarOnColor?: DefaultMantineColor,
    //toolbarOnShade?: 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9,

    filtersBadgeSize?: MantineSize,
    filtersBadgeVariant?: BadgeVariant,

    showAppliedFilters?: boolean,

    showRefreshButton?: boolean,
    showSelectRowsButton?: boolean,
    showFiltersButton?: boolean,
    showSearchInput?: boolean,


    visibleFilters?: string[],
    initialColumnFilters?: ColumnsObject,

    filtersTitle?: string,
    editFitersLabel?: string,
    clearFiltersLabel?: string,

    filtersAccordionVariant?: AccordionVariant,

    showColumnsConfig?: boolean,
    configMenuOpened?: boolean,
    setConfigMenuOpened?: Dispatch<SetStateAction<boolean>>,
    configMenuDropdown?: ReactNode,
}

export const DataGridToolbarDefaultProps: Partial<DataGridToolbarOptions> = {
    size: "sm",
    hideCheckboxToggle: false,
    toolbarIconVariant: "light",
    enableExport: true,
    searchPlaceholder: "Enter search text",
    refreshTooltip: "Refresh",
    exportTooltip: "Export",
    selectRowsTooltip: "Select items",
    filterTooltip: "Filter items",
    clearFiltersTooltip: "Clear filters",
    importTooltip: "Import data from .CSV file",

    //toolbarOnColor: "green",
    //toolbarOnShade: 8,

    filtersBadgeSize: "lg",
    showAppliedFilters: true,
    showRefreshButton: true,
    showFiltersButton: true,
    showSearchInput: true,
    showSelectRowsButton: true,

    filtersTitle: "Applied filters",
    filtersAccordionVariant: "contained",
    filtersBadgeVariant: "outline",

    editFitersLabel: "Edit filters",
    clearFiltersLabel: "Clear filters",

}


export function DataGridToolbar(props: DataGridToolbarOptions) {

    const {
        size, hideCheckboxToggle, onCheckboxToggle,
        onSearchTextChange, onRefreshClick, autoFocus,
        toolbarIconVariant, onExportClick, enableExport,
        searchPlaceholder, refreshTooltip, exportTooltip, selectRowsTooltip,
        searchData, setSearchData, searchText, setSearchText, filterTooltip, parentKeys,
        setFilterValues, filterValues, clearFiltersTooltip, filtersFormSize, FiltersEntity, client,
        enableImport, importTooltip, onImportClick, filtersBadgeSize,
        showAppliedFilters, showRefreshButton, showFiltersButton, visibleFilters,
        filtersDescription, setFiltersDescription, initialColumnFilters, filtersTitle,
        editFitersLabel, clearFiltersLabel, filtersAccordionVariant, filtersBadgeVariant,
        showSearchInput, showSelectRowsButton, showColumnsConfig, configMenuOpened, setConfigMenuOpened,
        configMenuDropdown
    } = useComponentDefaultProps('DataGridToolbar', DataGridToolbarDefaultProps, props);

    const theme = useMantineTheme();

    const { buttonsSize, actionIconSize, iconsSize, badgeSize } = getToolbarSizes(size!);

    const filtersAPI = useDataGridToolbarFilters({
        filterValues, setFilterValues, filtersDescription, setFiltersDescription, client: client, parentKeys, FiltersEntity, setSearchData, setSearchText, onRefreshClick, searchData, onSearchTextChange,
        filtersFormSize, visibleFilters, initialColumnFilters
    });

    const appliedFiltersCount = filtersAPI.filtersDescription ? Object.values(filtersAPI.filtersDescription).filter(value => value?.toString() !== '').length : 0;

    return (
        <>
            <Group>
                {showRefreshButton &&
                    <ActionIcon
                        title={refreshTooltip}
                        onClick={() => {
                            if (onRefreshClick) onRefreshClick(searchText)
                        }}
                        size={actionIconSize}
                        radius="xl"
                        color={theme.primaryColor}
                        variant={toolbarIconVariant} >
                        <IconReload size={iconsSize} stroke="1.5" />
                    </ActionIcon>
                }
                {showSearchInput &&
                    <SearchFilterInput
                        placeholder={searchPlaceholder}
                        onKeyDown={(e: React.KeyboardEvent) => filtersAPI.handleSearchFilterInputEnter(e)}

                        data={searchData}
                        value={searchText}
                        searchValue={filtersAPI.queryText}

                        onSearchClick={() => onRefreshClick(searchText)}
                        onChange={filtersAPI.handleSearchFilterInputOnChange}
                        onSearchChange={(text) => filtersAPI.setQueryText(text)}
                        onCreate={filtersAPI.createSearchPhrase}

                        clearSearchOnChange

                        size={buttonsSize}
                        iconsSize={iconsSize}
                        autoFocus={autoFocus}
                        iconVariant={toolbarIconVariant}
                        sx={{ flexGrow: 1 }}
                    />
                }
                {showFiltersButton && FiltersEntity &&
                    <>
                        {filtersAPI.filtersDescription && Object.keys(filtersAPI.filtersDescription).length > 0 &&
                            <Badge title={filterTooltip} pl={0} radius="xl" size={badgeSize} maw="20rem" fz="xs"
                                styles={{ root: { textTransform: 'unset', fontWeight: 400 } }}
                                leftSection={
                                    <ActionIcon onClick={filtersAPI.handleFilterButtonClick} size={actionIconSize} radius="xl" variant={toolbarIconVariant} color={theme.primaryColor}>
                                        <IconFilter size={iconsSize} stroke="1.5" />
                                    </ActionIcon>
                                }
                                rightSection={
                                    <ActionIcon size="xs" color={theme.primaryColor} title={clearFiltersTooltip} radius="xl" variant="transparent" onClick={filtersAPI.handleClearFiltersClick}>
                                        <IconX size={rem(10)} />
                                    </ActionIcon>}
                            >
                                ({appliedFiltersCount})
                            </Badge>
                        }
                        {(!filtersAPI.filtersDescription || (filtersAPI.filtersDescription && Object.keys(filtersAPI.filtersDescription).length === 0)) &&
                            <ActionIcon title={filterTooltip} onClick={filtersAPI.handleFilterButtonClick} size={actionIconSize} radius="xl" color={theme.primaryColor} variant={toolbarIconVariant}>
                                <IconFilter size={iconsSize} stroke="1.5" />
                            </ActionIcon>
                        }
                    </>
                }
                {showSelectRowsButton &&
                    <ToggleActionIcon
                        title={selectRowsTooltip}
                        hidden={hideCheckboxToggle ? true : false}
                        onClick={onCheckboxToggle}
                        size={actionIconSize}
                        offColor={theme.primaryColor}
                        onColor={theme.primaryColor}
                        offVariant={toolbarIconVariant}
                        onVariant={toolbarIconVariant}
                        onIcon={<IconSquareCheckFilled size={iconsSize} stroke="1.5" />}
                        offIcon={<IconSquareCheck size={iconsSize} stroke="1.5" />}
                    />
                }
                {enableExport &&
                    <ActionIcon
                        title={exportTooltip}
                        onClick={() => {
                            if (onExportClick) onExportClick()
                        }} size={actionIconSize} radius="xl" color={theme.primaryColor} variant={toolbarIconVariant} ><IconDownload size={iconsSize} stroke="1.5" /></ActionIcon>
                }
                {enableImport && onImportClick &&
                    <ActionIcon
                        title={importTooltip}
                        onClick={() => {
                            if (onImportClick) onImportClick()
                        }} size={actionIconSize} radius="xl" color={theme.primaryColor} variant={toolbarIconVariant} ><IconCloudUpload size={iconsSize} stroke="1.5" /></ActionIcon>
                }
                {showColumnsConfig && setConfigMenuOpened && configMenuDropdown &&
                    <Menu opened={configMenuOpened} onChange={setConfigMenuOpened} withinPortal closeOnItemClick={false}>
                        <Menu.Target>
                            <ActionIcon
                                title="Configure columns"
                                size={actionIconSize} radius="xl" color={theme.primaryColor} variant={toolbarIconVariant} >
                                <IconEyeCog size={iconsSize} stroke="1.5" />
                            </ActionIcon>
                        </Menu.Target>
                        {configMenuDropdown}
                    </Menu>
                }
            </Group>
            {showAppliedFilters && FiltersEntity && filtersAPI.filtersDescription && Object.keys(filtersAPI.filtersDescription).length > 0 &&
                <>
                    <Accordion defaultValue="filters" mt="sm" variant={filtersAccordionVariant}>
                        <Accordion.Item value="filters">
                            <Accordion.Control>
                                <Group position="apart">
                                    <Text size="xs" weight={500}>{appliedFiltersCount > 0 ? `(${appliedFiltersCount}) ` : ''}{filtersTitle}</Text>
                                    <Group position="right">
                                        <ActionIcon
                                            component="a"
                                            size={actionIconSize} radius="xl" variant="default" title={clearFiltersLabel}
                                            onClick={filtersAPI.handleClearFiltersClick}
                                        >
                                            <IconFilterOff size={iconsSize} stroke="1.5" />
                                        </ActionIcon>
                                        <ActionIcon
                                            component="a"
                                            size={actionIconSize} radius="xl" color={theme.primaryColor} variant={toolbarIconVariant} title={editFitersLabel}
                                            onClick={filtersAPI.handleFilterButtonClick}
                                        >
                                            <IconPencil size={iconsSize} stroke="1.5" />
                                        </ActionIcon>
                                    </Group>
                                </Group>
                            </Accordion.Control>
                            <Accordion.Panel>
                                <Stack spacing="xs">
                                    <Group spacing="xs" style={{ gap: '0.5rem' }}>
                                        {Object.keys(filtersAPI.filtersDescription).map((filter, index) =>
                                        (
                                            filtersAPI.filtersDescription?.[filter]?.toString() ?
                                                <Badge
                                                    component="a"
                                                    key={filter} radius="sm" size={filtersBadgeSize} maw="20rem"
                                                    styles={{ root: { textTransform: 'unset', cursor: 'pointer' } }} //#cbe6d0
                                                    onClick={filtersAPI.handleFilterButtonClick}
                                                    variant={filtersBadgeVariant}
                                                >
                                                    {filter}{filtersAPI.filtersDescription?.[filter] ? `: ${filtersAPI.filtersDescription?.[filter]?.toString()}` : ''}
                                                </Badge>
                                                : null
                                        )
                                        )}
                                    </Group>
                                </Stack>
                            </Accordion.Panel>
                        </Accordion.Item>
                    </Accordion>
                </>
            }
        </>
    );
}