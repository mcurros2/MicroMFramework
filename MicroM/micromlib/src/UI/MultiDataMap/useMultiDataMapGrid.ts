import { MantineTheme, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ValuesRecord } from "../../client";
import { latLng, useEntityUI, useExecuteView, useModal, useViewState } from "../Core";
import { useDataGrid } from "../DataGrid/useDatagrid";
import { DataMapMarkerProps } from "../DataMap";
import { DataMapDefaultDataGridProps } from "../DataMap/DataMap";
import { MapMarkerGroupDefaultFillColor, MapMarkerGroupDefaultLabelOrigin, MapMarkerGroupDefaultSVGIcon, useCreateMarkerOptions } from "../DataMap/useCreateMarkerOptions";
import { LocationGroup, RecordGroupManager, useRecordGroupManager } from "../DataMap/useRecordGroupManager";
import { DefaultMapClusterProps, MarkerProps, SelectedMarkerProps } from "../GoogleMaps";
import { GridSelection, GridSelectionMode } from "../Grid";
import { MultiDataMapViewProps } from "./MultiDataMap";

export interface UseMultiDataMapGridProps {
    dataMapView: MultiDataMapViewProps,
    viewState: ReturnType<typeof useViewState>,
    selectionMode?: GridSelectionMode,
    mapReady: boolean,
    placesReady: boolean,
    geocoderReady: boolean,
    clearSelectionOnActionExecuted?: boolean
}
export function useMultiDataMapGrid({
    dataMapView, viewState, selectionMode, mapReady, placesReady, geocoderReady, clearSelectionOnActionExecuted
}: UseMultiDataMapGridProps) {
    const theme = useMantineTheme();
    const modal = useModal();

    const [selectedRows, setSelectedRows] = useState<GridSelection>([]);
    const [selectedMarkers, setSelectedMarkers] = useState<SelectedMarkerProps>({});
    const [showOnMap, setShowOnMap] = useState(true);

    const defaultProps = useComponentDefaultProps('MultiDataMapView', DataMapDefaultDataGridProps, dataMapView);

    // Handler for clearingSelection after executing an action
    const handleActionExecuted = useCallback((actionName: string, result?: boolean) => {
        if (defaultProps.onActionExecuted) defaultProps.onActionExecuted(actionName, result);
        if (clearSelectionOnActionExecuted) if (result) setSelectedRows([]);
    }, [clearSelectionOnActionExecuted, defaultProps]);

    const dgProps = useMemo(() => ({ ...defaultProps, onActionExecuted: handleActionExecuted, selectionMode: selectionMode || 'multi' }), [defaultProps, handleActionExecuted, selectionMode]);

    const {
        entity, viewName, columnsOverrides,
        latitudRecordIndex, longitudRecordIndex,
        markerRenderer, InfoWindowRenderer, groupMarkerRenderer, selectRecordsRenderer, parentKeys, centerMarkerColumnIndex
    } = dgProps;

    const groupManager = useRecordGroupManager();

    const { searchText, limitRows, refresh, filterValues } = viewState;
    const executeViewState = useExecuteView(entity, parentKeys, viewName, searchText, limitRows, refresh, filterValues);

    const dataGridAPI = useDataGrid(dgProps, { executeViewState, setRefresh: viewState.setRefresh, setSearchText: viewState.setSearchText });

    const actualColumnsOverrides = useMemo(() => {
        if (!entity || !viewName) {
            if (!viewName && entity) console.warn(`MultiDataMapView: viewName not specified for ${entity?.name}`);
            return undefined;
        }
        return columnsOverrides ? columnsOverrides : entity.def.views[viewName].gridColumnsOverrides?.(theme);
    }, [columnsOverrides, entity, theme, viewName]);

    const getMarkerOptions = useCreateMarkerOptions();

    // Default selected records renderer
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
    }, [setSelectedRows]);

    // Click handler for selection and Infowindow
    const actualInfoWindowRenderer = useMemo(() =>
        ((entity && viewName) ? entity.def.views[viewName].mapInfoWindowRenderer : InfoWindowRenderer)
        , [InfoWindowRenderer, entity, viewName]);

    const actualSelectionClickHandler = useMemo(() =>
        ((entity && viewName) ? entity.def.views[viewName].mapSelectRecordsRenderer : selectRecordsRenderer) ?? DefaultSelectRecordsRenderer
        , [entity, viewName, selectRecordsRenderer, DefaultSelectRecordsRenderer]);

    // InfoWindow Click handler
    const getInfoWindowClickHandler = useCallback((dataSetId: string, record: ValuesRecord, recordIndex: number) => {
        if (!latitudRecordIndex || !longitudRecordIndex) return null;

        const position = { lat: record[latitudRecordIndex] as number, lng: record[longitudRecordIndex] as number };

        if (dataGridAPI.showSelectCheckbox) {
            if (!actualSelectionClickHandler) return null;
            const clickHandler = (infoWindow: google.maps.InfoWindow) => {
                return actualSelectionClickHandler(record, recordIndex, position, groupManager, infoWindow, dataGridAPI.UIAPI, theme, entity!);
            }
            return clickHandler;
        }
        else {
            if (!actualInfoWindowRenderer) return null;
            const clickHandler = (infoWindow: google.maps.InfoWindow) => {
                return actualInfoWindowRenderer(record, position, groupManager, infoWindow, dataGridAPI.UIAPI, theme, entity!, modal);

            }
            return clickHandler;
        }

    }, [actualInfoWindowRenderer, actualSelectionClickHandler, dataGridAPI.UIAPI, dataGridAPI.showSelectCheckbox, groupManager, latitudRecordIndex, longitudRecordIndex, theme, modal]);

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

    const actualGroupMarkerRenderer = useMemo(() => {
        if (entity && viewName) {
            return entity.def.views[viewName]?.mapGroupMarkerRenderer ?? groupMarkerRenderer ?? defaultGroupMarkerRenderer;
        }
        return groupMarkerRenderer ?? defaultGroupMarkerRenderer;
    }, [defaultGroupMarkerRenderer, entity, groupMarkerRenderer, viewName]);

    // Marker renderer
    const actualMarkerRenderer = useMemo(() => {
        if (!entity || !viewName) return undefined;
        return entity.def.views[viewName].mapMarkerRenderer ?? markerRenderer;
    }, [entity, markerRenderer, viewName]);


    const [centerMarker, setCenterMarker] = useState<MarkerProps | undefined>(undefined);
    const [markers, setMarkers] = useState<MarkerProps[]>([]);

    useEffect(() => {
        if (!viewName) return;
        if (!showOnMap) {
            setMarkers([]);
            return;
        };
        if (!mapReady || !placesReady || !geocoderReady) return;
        if (!actualGroupMarkerRenderer) return;
        if (!latitudRecordIndex || !longitudRecordIndex) {
            console.warn(`MultiDataMapView: latitudRecordIndex or longitudRecordIndex not specified in ${entity?.name} view: ${viewName}`);
            return;
        }

        if (!executeViewState.data || executeViewState.data.length === 0 || !executeViewState.data[0].records || executeViewState.data[0].records.length === 0) {
            groupManager.clearAllGroups();
            setMarkers([]);
            return;
        }

        const records = executeViewState.data[0].records;

        groupManager.clearAllGroups();

        // Get the groups and marker options
        const processedMarkerCreateOptions: DataMapMarkerProps[] = [];

        for (let i = 0; i < records.length; i++) {
            const record = records[i];

            if (!record[latitudRecordIndex!] || !record[longitudRecordIndex!]) continue; // skip records that have no location

            const position = { lat: record[latitudRecordIndex!] as number, lng: record[longitudRecordIndex!] as number };

            const markerCreateOptions = actualMarkerRenderer ? actualMarkerRenderer(record, position, theme) : {
                position: position,
                title: `${record[0]}`,
                fillColor: 'red'
            };

            if (markerCreateOptions === null) continue;

            markerCreateOptions.dataSetId = dataMapView.dataSetId;
            markerCreateOptions.recordIndex = i;
            markerCreateOptions.isMapCenter = (centerMarkerColumnIndex && record[centerMarkerColumnIndex]) ? true : undefined;

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

                const markerCreateOptions = actualGroupMarkerRenderer(group, createMarkerRecord, theme);

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

            const clickHandler = getInfoWindowClickHandler(dataMapView.dataSetId!, records[createMarkerRecord.recordIndex!], createMarkerRecord.recordIndex!);
            if (clickHandler) markerOptions.clickable = true;

            if (createMarkerRecord.isMapCenter) setCenterMarker({ markerOptions, selectedMarkerOptions });

            newMarkers.push({
                markerOptions,
                selectedMarkerOptions,
                onMarkerClick: clickHandler ?? undefined,
                recordIndex: group ? undefined : createMarkerRecord.recordIndex,
                dataSetId: dataMapView.dataSetId ?? dataMapView.tabLabel ?? dataMapView.viewName,
                isMapCenter: createMarkerRecord.isMapCenter
            });
        }

        setMarkers(newMarkers);

    }, [executeViewState.data, latitudRecordIndex, longitudRecordIndex, dataGridAPI.showSelectCheckbox, showOnMap, mapReady, placesReady, geocoderReady, centerMarkerColumnIndex, viewName]);


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
        }, {} as SelectedMarkerProps);

        setSelectedMarkers(currentSelection);

    }, [selectedRows]); // el resto de las dependencias está intencionalmente dejado fuera

    return {
        dgProps,
        viewState,
        executeViewState,
        dataGridAPI,
        actualColumnsOverrides,
        //actualInfoWindowRenderer,
        actualSelectionClickHandler,
        selectedRows,
        setSelectedRows,
        getInfoWindowClickHandler,
        //actualGroupMarkerRenderer,
        //actualMarkerRenderer,
        markers: markers,
        selectedMarkers,
        showOnMap,
        setShowOnMap,
        centerMarker
    }
}