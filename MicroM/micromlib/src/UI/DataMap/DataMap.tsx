import { Group, MantineTheme, Select, SelectItem, Stack, Tabs, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconGridDots, IconMap } from "@tabler/icons-react";
import { ReactNode, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { ValuesRecord } from "../../client";
import { FakeProgressBar, ModalContextType, latLng, useEntityUI, useExecuteView, useModal, useViewState } from "../Core";
import { DataGridDefaultProps, DataGridProps, DataGridToolbar } from "../DataGrid";
import { DataGridActionsToolbar } from "../DataGrid/DataGridActionsToolbar";
import { useDataGrid } from "../DataGrid/useDatagrid";
import { DataViewLimitData } from "../DataView";
import { DEFAULT_MAP_CENTER, MapOptions, MarkerProps } from "../GoogleMaps";
import { DefaultMapClusterProps, GoogleMapCluster, MapClusterOptions, SelectedMarkerProps } from "../GoogleMaps/GoogleMapCluster";
import { GoogleMapsInfoWindow } from "../GoogleMaps/GoogleMapsInfoWindow";
import { GridSelection } from "../Grid";
import { DataMapGrid } from "./DataMapGrid";
import { MapMarkerGroupDefaultFillColor, MapMarkerGroupDefaultLabelOrigin, MapMarkerGroupDefaultSVGIcon, UseCreateMarkerOptionsProps, useCreateMarkerOptions } from "./useCreateMarkerOptions";
import { LocationGroup, RecordGroupManager, useRecordGroupManager } from "./useRecordGroupManager";


export interface DataMapMarkerProps extends UseCreateMarkerOptionsProps {
}

export interface DataMapSVGIcon {
    svgIcon: string,
    svgSelectedIcon?: string
}

export type DataMapInfoWindowRenderer = (
    record: ValuesRecord,
    position: latLng,
    groupManager: RecordGroupManager,
    infoWindow: google.maps.InfoWindow,
    UIAPI: ReturnType<typeof useEntityUI>,
    theme: MantineTheme,
    entity: Entity<EntityDefinition>,
    modal: ModalContextType
) => ReactNode;

export type DataMapSelectRecordsRenderer = (
    record: ValuesRecord,
    recordIndex: number,
    position: latLng,
    groupManager: RecordGroupManager,
    infoWindow: google.maps.InfoWindow,
    UIAPI: ReturnType<typeof useEntityUI>,
    theme: MantineTheme,
    entity: Entity<EntityDefinition>
) => ReactNode | null;

export type DataMapMarkerRenderer = (record: ValuesRecord, position: latLng, theme: MantineTheme) => DataMapMarkerProps | null;

export type DataMapGroupMarkerRenderer = (group: LocationGroup, markerOptions: DataMapMarkerProps, theme: MantineTheme) => DataMapMarkerProps | null;
export interface DataMapProps {
    dataGridProps: Omit<DataGridProps, 'gridHeight'>,
    latitudRecordIndex: number,
    longitudRecordIndex: number,
    mapStyle?: google.maps.MapTypeStyle[],
    mapOptions?: google.maps.MapOptions,
    markerClustererOptions?: MapClusterOptions,
    markerRenderer?: DataMapMarkerRenderer,
    groupMarkerRenderer?: DataMapGroupMarkerRenderer,
    InfoWindowRenderer?: DataMapInfoWindowRenderer,
    selectRecordsRenderer?: DataMapSelectRecordsRenderer,
    labels?: {
        gridTabLabel?: string,
        mapTabLabel?: string,
    }
    mapHeight?: string | number,
    centerMarker?: DataMapMarkerProps,
    onDataRefresh?: () => void
}

export const DataMapDefaultProps: Partial<DataMapProps> = {
    labels: {
        gridTabLabel: 'Data',
        mapTabLabel: 'Map',
    },
    mapStyle: [
        {
            featureType: 'poi',
            stylers: [{ visibility: 'off' }],
        },
        {
            featureType: 'transit',
            stylers: [{ visibility: 'off' }],
        },
    ],
    mapOptions: {
        zoom: 10,
        fullscreenControl: true,
        center: DEFAULT_MAP_CENTER,
        gestureHandling: 'greedy'
    },
    mapHeight: 'auto'
}


export const DataMapDefaultDataGridProps: Partial<DataGridProps> = { ...DataGridDefaultProps, autoSelectFirstRow: false, renderOnlyWhenVisible: false };

export function DataMap(props: DataMapProps) {
    const {
        dataGridProps, labels: dataMapLabels,
        mapOptions, mapStyle, mapHeight, markerRenderer, InfoWindowRenderer,
        latitudRecordIndex, longitudRecordIndex, groupMarkerRenderer, selectRecordsRenderer, centerMarker
    } = useComponentDefaultProps('DataMap', DataMapDefaultProps, props);

    const effectiveDatagridProps = useComponentDefaultProps('DataGrid', DataMapDefaultDataGridProps, dataGridProps);

    const {
        search, limit, entity, parentKeys, viewName, labels,
        enableAdd, enableEdit, enableDelete, enableView, enableExport,
        autoFocus, actionsButtonVariant, toolbarIconVariant, toolbarSize,
        showActions, filtersFormSize, renderOnlyWhenVisible, columnsOverrides,
        setInitialFiltersFromColumns, visibleFilters
    } = effectiveDatagridProps;

    const theme = useMantineTheme();
    const modal = useModal();

    // infoWindow
    const infoWindowContentRef = useRef(document.createElement('div'));
    const [infoWindowContent, setInfoWindowContent] = useState<React.ReactNode>(null);

    // Map
    const googleMapsAPI = useGoogleMapsAPI();

    const { mapReady, placesReady, geocoderReady } = googleMapsAPI;

    const currentMapOptions = useMemo<MapOptions>(() => ({
        ...mapOptions, styles: mapStyle
    }), [mapOptions, mapStyle]);

    const [selectedMarkers, setSelectedMarkers] = useState<SelectedMarkerProps>({});

    // Grid

    //toolbar
    const [searchData, setSearchData] = useState<SelectItem[]>(search?.map(s => { return { value: s, label: s } }) as SelectItem[]);
    const [selectedRows, setSelectedRows] = useState<GridSelection>([]);

    const viewState = useViewState(search, limit);

    const executeViewState = useExecuteView(entity, parentKeys, viewName, viewState.searchText, viewState.limitRows, viewState.refresh, viewState.filterValues);

    const dataGridAPI = useDataGrid(effectiveDatagridProps, { executeViewState, setRefresh: viewState.setRefresh, setSearchText: viewState.setSearchText });

    const { isLoading, rows } = dataGridAPI;

    const { UIAPI } = dataGridAPI;


    const getMarkerOptions = useCreateMarkerOptions();

    const groupManager = useRecordGroupManager();

    const TN = {
        grid: 'gridTab', gridIcon: <IconGridDots size="1rem" />,
        map: 'mapTab', mapIcon: <IconMap size="1rem" />,
    };

    const effectiveColumnOverrides = useMemo(() => {
        if (!entity || !viewName) return undefined;
        return columnsOverrides ? columnsOverrides : entity.def.views[viewName].gridColumnsOverrides?.(theme);
    }, [columnsOverrides, entity, theme, viewName]);
    //


    // Markers handling

    // Default select records renderer
    const DefaultSelectRecordsRenderer = useCallback((record: ValuesRecord, recordIndex: number, position: latLng, groupManager: RecordGroupManager, infoWindow: google.maps.InfoWindow, UIAPI: ReturnType<typeof useEntityUI>, theme: MantineTheme) => {
        if (recordIndex === undefined || recordIndex === null) return;

        setSelectedRows((prevSelectedRows) => {
            const recid = recordIndex + 1;
            const isAlreadySelected = prevSelectedRows.some(row => row.recid === recid);

            if (isAlreadySelected) {
                return prevSelectedRows.filter(row => row.recid !== recid);
            } else {
                return [...prevSelectedRows, { ...record, recid }];
            }
        });

        return null;
    }, []);

    // Click handler for selection and Infowindow
    const effectiveInfoWindowRenderer = useMemo(() =>
        ((entity && viewName) ? entity.def.views[viewName].mapInfoWindowRenderer : InfoWindowRenderer)
        , [InfoWindowRenderer, entity, viewName]);

    const effectiveSelectionClickHandler = useMemo(() =>
        ((entity && viewName) ? entity.def.views[viewName].mapSelectRecordsRenderer : selectRecordsRenderer) ?? DefaultSelectRecordsRenderer
        , [DefaultSelectRecordsRenderer, entity, selectRecordsRenderer, viewName]);

    const getInfoWindowClickHandler = useCallback((record: ValuesRecord, recordIndex: number) => {
        if (!latitudRecordIndex || !longitudRecordIndex) return null;

        if (!record || recordIndex === undefined || recordIndex === null) return null;
        const position = { lat: record[latitudRecordIndex] as number, lng: record[longitudRecordIndex] as number };

        if (dataGridAPI.showSelectCheckbox) {
            if (!effectiveSelectionClickHandler) return null;
            const clickHandler = (infoWindow: google.maps.InfoWindow) => {
                return effectiveSelectionClickHandler(record, recordIndex, position, groupManager, infoWindow, UIAPI, theme, entity!);
            }
            return clickHandler;
        }
        else {
            if (!effectiveInfoWindowRenderer) return null;
            const clickHandler = (infoWindow: google.maps.InfoWindow) => {
                return effectiveInfoWindowRenderer(record, position, groupManager, infoWindow, UIAPI, theme, entity!, modal);

            }
            return clickHandler;
        }

    }, [UIAPI, dataGridAPI.showSelectCheckbox, effectiveInfoWindowRenderer, effectiveSelectionClickHandler, groupManager, latitudRecordIndex, longitudRecordIndex, theme]);

    // Group Markers
    const defaultGroupMarkerRenderer = useCallback((group: LocationGroup, markerOptions: DataMapMarkerProps, theme: MantineTheme) => {
        const markerCreateOptions = markerOptions;

        markerCreateOptions.svgIcon = MapMarkerGroupDefaultSVGIcon;
        markerCreateOptions.fillColor = MapMarkerGroupDefaultFillColor;
        markerCreateOptions.assignColorByValue = undefined;
        markerCreateOptions.title = `${group.records.length} ${DefaultMapClusterProps.labels?.locationsLabel}`;

        markerCreateOptions.label = { text: group.records.length.toString(), color: 'white', fontSize: '0.7rem', fontWeight: '500' }
        markerCreateOptions.labelOrigin = new google.maps.Point(MapMarkerGroupDefaultLabelOrigin.x, MapMarkerGroupDefaultLabelOrigin.y);

        return markerCreateOptions;
    }, []);

    const effectiveGroupMarkerRenderer = useMemo(() =>
        ((entity && viewName) ? entity.def.views[viewName].mapGroupMarkerRenderer : groupMarkerRenderer) ?? defaultGroupMarkerRenderer
        , [defaultGroupMarkerRenderer, entity, groupMarkerRenderer, viewName]);


    // newMarkers
    const effectiveMarkerRenderer = useMemo(() => {
        if (!entity || !viewName) return undefined;
        return entity.def.views[viewName].mapMarkerRenderer ?? markerRenderer;
    }, [entity, markerRenderer, viewName]);


    const [markers, setMarkers] = useState<MarkerProps[]>([]);

    useEffect(() => {
        if (!mapReady || !placesReady || !geocoderReady) return;
        if (!executeViewState.data || executeViewState.data.length === 0 || !executeViewState.data[0].records || executeViewState.data[0].records.length === 0) return;
        if (!effectiveGroupMarkerRenderer) return;
        if (!latitudRecordIndex || !longitudRecordIndex) {
            console.warn(`DataMap: latitudRecordIndex or longitudRecordIndex not specified in ${entity?.name} view: ${viewName}`);
            return;
        }

        const records = executeViewState.data[0].records;

        groupManager.clearAllGroups();

        // Get the groups and marker options
        const processedMarkerCreateOptions: DataMapMarkerProps[] = [];

        // To spiderify newMarkers with the same location. Number is the of marker rendered so far to multiply by the offset
        //const processedLocations: Record<string, DataMapMarkerProps & { markersCount: number }> = {};

        if (centerMarker) processedMarkerCreateOptions.push(centerMarker);

        for (let i = 0; i < records.length; i++) {
            const record = records[i];
            if (latitudRecordIndex === -1 || longitudRecordIndex === -1) continue;
            if (!record[latitudRecordIndex!] || !record[longitudRecordIndex!]) continue; // skip records that have no location

            const position = { lat: record[latitudRecordIndex] as number, lng: record[longitudRecordIndex] as number };

            // Ignore invalid newMarkers
            if (!position.lat || !position.lng) continue;

            const markerCreateOptions = effectiveMarkerRenderer ? effectiveMarkerRenderer(record, position, theme) : {
                position: position,
                title: `${record[0]}`,
                fillColor: 'red'
            };

            if (markerCreateOptions === null) continue;

            markerCreateOptions.recordIndex = i;
            groupManager.addRecordToGroup(markerCreateOptions.position, i, record);

            processedMarkerCreateOptions.push(markerCreateOptions);
        }

        // delete groups with only one record
        groupManager.clearOneRecordGroups();

        // Create the newMarkers
        const renderedGroups: { [key: string]: boolean } = {};

        const newMarkers: MarkerProps[] = [];
        for (let i = 0; i < processedMarkerCreateOptions.length; i++) {
            const createMarkerRecord = processedMarkerCreateOptions[i];

            const group = groupManager.getGroupByLocation(createMarkerRecord.position);


            let markerOptions: google.maps.MarkerOptions;
            let selectedMarkerOptions: google.maps.MarkerOptions | undefined;

            if (group) {
                // Check if the group was already rendered
                if (renderedGroups[group.locationKey]) continue;
                renderedGroups[group.locationKey] = true;

                const markerCreateOptions = effectiveGroupMarkerRenderer(group, createMarkerRecord, theme);

                if (markerCreateOptions === null) continue;

                const optionsResult = getMarkerOptions(markerCreateOptions);
                markerOptions = optionsResult.markerOptions;
                selectedMarkerOptions = optionsResult.selectedMarkerOptions;

            }
            else {
                const optionsResult = getMarkerOptions(createMarkerRecord);
                markerOptions = optionsResult.markerOptions;
                selectedMarkerOptions = optionsResult.selectedMarkerOptions;
            }

            const clickHandler = getInfoWindowClickHandler(records[createMarkerRecord.recordIndex!], createMarkerRecord.recordIndex!);
            if (clickHandler) markerOptions.clickable = true;

            newMarkers.push({
                markerOptions,
                selectedMarkerOptions,
                onMarkerClick: clickHandler ?? undefined,
                recordIndex: group ? undefined : createMarkerRecord.recordIndex,
                dataSetId: 'default'
            });

        }

        setMarkers(newMarkers);

    }, [executeViewState.data, latitudRecordIndex, longitudRecordIndex, dataGridAPI.showSelectCheckbox, centerMarker]);
    // intentionally left out mapReady, placesReady and geocoderReady dependencies to avoid re-rendering the map when the map is not ready yet

    // newMarkers selection
    useEffect(() => {

        if (selectedRows.length === 0) {
            setSelectedMarkers({});
            return;
        }

        const selectedRowsMap: Record<number, number> = {};
        selectedRows.forEach((row, index) => {
            selectedRowsMap[row.recid] = index;
        });

        // Filtrar y mapear los newMarkers seleccionados
        const currentSelection = markers.reduce((acc: SelectedMarkerProps, marker) => {
            if (marker.recordIndex !== undefined && selectedRowsMap[marker.recordIndex + 1] !== undefined) {
                const dataSetId = marker.dataSetId!;
                if (!acc[dataSetId]) acc[dataSetId] = {};

                marker.selectedMarkerOptions!.label = selectedRowsMap[marker.recordIndex + 1].toString();
                marker.selectedOrder = selectedRowsMap[marker.recordIndex + 1];

                acc[dataSetId][marker.recordIndex] = marker;
            }
            return acc;
        }, {});


        setSelectedMarkers(currentSelection);

    }, [selectedRows]); // intentionally left out markers dependencies to avoid re-rendering the map when the markers are not ready yet

    return (
        <>
            <DataGridToolbar
                {...labels!}

                enableExport={enableExport}
                onExportClick={dataGridAPI.handleExport}

                onRefreshClick={dataGridAPI.handleRefresh}
                onCheckboxToggle={dataGridAPI.handleToggleSelectable}
                autoFocus={autoFocus}
                toolbarIconVariant={toolbarIconVariant}
                size={toolbarSize}

                searchText={viewState.searchText}
                setSearchText={viewState.setSearchText}
                searchData={searchData}
                setSearchData={setSearchData}

                client={entity?.API.client}

                FiltersEntity={(entity && viewName) ? entity?.def.views[viewName].FiltersEntity : undefined}

                filterValues={viewState.filterValues}
                setFilterValues={viewState.setFilterValues}

                filtersDescription={viewState.filtersDescription}
                setFiltersDescription={viewState.setFiltersDescription}

                filtersFormSize={filtersFormSize!}

                initialColumnFilters={setInitialFiltersFromColumns && entity ? entity.def.columns : undefined}
            />
            <Tabs pt="xs" defaultValue={TN.map}>
                <Tabs.List>
                    <Tabs.Tab value={TN.map} icon={TN.mapIcon} >{dataMapLabels?.mapTabLabel}</Tabs.Tab>
                    <Tabs.Tab value={TN.grid} icon={TN.gridIcon} >{dataMapLabels?.gridTabLabel}</Tabs.Tab>
                </Tabs.List>
                <Tabs.Panel value={TN.grid} pt="xs">
                    <DataMapGrid
                        {...effectiveDatagridProps}
                        columnsOverrides={effectiveColumnOverrides}
                        selectedRows={selectedRows}
                        setSelectedRows={setSelectedRows}
                        executeViewState={executeViewState}
                        dataGridAPI={dataGridAPI}
                        viewState={viewState}
                        renderOnlyWhenVisible={renderOnlyWhenVisible}
                        gridHeight={mapHeight}
                        formMode={effectiveDatagridProps.parentFormAPI?.formMode}
                    />
                </Tabs.Panel>
                <Tabs.Panel value={TN.map} pt='xs'>
                    <Stack spacing="sm">
                        <DataGridActionsToolbar
                            {...labels!}
                            size={toolbarSize}
                            viewName={viewName}

                            onAddClick={dataGridAPI.handleAddClick}
                            onEditClick={dataGridAPI.handleEditClick}
                            onDeleteClick={dataGridAPI.handleDeleteClick}
                            onViewClick={dataGridAPI.handleViewClick}

                            enableAdd={enableAdd}
                            enableEdit={enableEdit}
                            enableDelete={enableDelete}
                            enableView={enableView}

                            actionsButtonVariant={actionsButtonVariant}
                            clientActions={entity ? entity.def.clientActions : {}}
                            handleExecuteAction={dataGridAPI.handleExecuteAction}
                            showActions={showActions}

                            parentFormMode={effectiveDatagridProps.parentFormAPI?.formMode}
                        />
                        {mapReady && placesReady && geocoderReady &&
                            <>
                                {((isLoading || executeViewState.loading) && markers.length === 0) &&
                                    <FakeProgressBar size="xs" />
                                }
                                <GoogleMapsInfoWindow container={infoWindowContentRef.current} infoWindowContent={infoWindowContent} />
                                <GoogleMapCluster
                                    style={{ height: mapHeight === 'auto' ? '50vh' : mapHeight }}
                                    mapOptions={currentMapOptions}
                                    infoWindowContentRef={infoWindowContentRef}
                                    setInfoWindowContent={setInfoWindowContent}
                                    markers={markers}
                                    selectedMarkers={selectedMarkers}
                                />

                            </>
                        }
                        <Group>
                            {executeViewState && !executeViewState.error &&
                                <>
                                    <Text fz="sm">{labels?.limitLabel}</Text>
                                    <Select size="xs" sx={{ maxWidth: '7rem' }}
                                        data={DataViewLimitData(labels!.rowsLabel, labels!.unlimitedLabel)}
                                        value={viewState.limitRows}
                                        onChange={viewState.setLimitRows}
                                    />
                                </>
                            }
                            {rows &&
                                <Text fz="xs" c="dimmed">{labels?.rowsReturnedLabel} {rows.length}{dataGridAPI.selectedRowsCount ? <>, {dataGridAPI.selectedRowsCount} {labels?.selectedRowsLabel}</> : ""}</Text>
                            }
                        </Group>
                    </Stack>
                </Tabs.Panel>
            </Tabs>

        </>
    )
}