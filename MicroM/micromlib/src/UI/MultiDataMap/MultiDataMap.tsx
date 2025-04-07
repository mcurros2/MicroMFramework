import { MantineNumberSize, SelectItem, Stack, Tabs, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconMap, IconProps } from "@tabler/icons-react";
import { ReactNode, useCallback, useEffect, useRef, useState } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { ActionIconVariant, FakeProgressBar, FormMode, useViewState } from "../Core";
import { DataGridProps, DataGridToolbar, DataGridToolbarSizes } from "../DataGrid";
import { DataMapGroupMarkerRenderer, DataMapInfoWindowRenderer, DataMapMarkerRenderer, DataMapSelectRecordsRenderer, useCreateMarkerOptions } from "../DataMap";
import { DataMapDefaultDataGridProps, DataMapMarkerProps } from "../DataMap/DataMap";
import { DataMapGrid } from "../DataMap/DataMapGrid";
import { DataViewLimit } from "../DataView";
import { DEFAULT_MAP_CENTER, MapOptions, MarkerProps } from "../GoogleMaps";
import { GoogleMapCluster, MapClusterOptions, SelectedMarkerProps } from "../GoogleMaps/GoogleMapCluster";
import { GoogleMapsInfoWindow } from "../GoogleMaps/GoogleMapsInfoWindow";
import { GridSelectionMode } from "../Grid";
import { MultiDataMapActionsToolbar } from "./MultiDataMapActionsToolbar";
import { useMultiDataMapGrid } from "./useMultiDataMapGrid";

export interface MultiDataMapViewProps extends Omit<DataGridProps, 'gridHeight' | 'selectionMode' | 'enableExport' | 'autoFocus' | 'toolbarIconVariant' | 'toolbarSize' | 'filtersFormSize' | 'parentFormAPI'> {
    latitudRecordIndex?: number,
    longitudRecordIndex?: number,
    dataSetId?: string,
    tabLabel?: string,
    tabIcon?: (props: IconProps) => ReactNode,
    InfoWindowRenderer?: DataMapInfoWindowRenderer,
    selectRecordsRenderer?: DataMapSelectRecordsRenderer,
    markerRenderer?: DataMapMarkerRenderer,
    groupMarkerRenderer?: DataMapGroupMarkerRenderer,
    centerMarkerColumnIndex?: number, // Column index on the recordset to get the center marker, if it has any value, it will be used as the center marker
}

export interface MultiDataMapProps {
    dataMapView1: MultiDataMapViewProps,
    dataMapView2?: MultiDataMapViewProps,
    dataMapView3?: MultiDataMapViewProps,
    dataMapView4?: MultiDataMapViewProps,
    dataMapView5?: MultiDataMapViewProps,

    search?: string[],
    limit?: DataViewLimit,
    enableExport?: boolean,
    autoFocus?: boolean,
    toolbarIconVariant?: ActionIconVariant,
    toolbarSize?: DataGridToolbarSizes,
    filtersFormSize?: MantineNumberSize,
    selectionMode?: GridSelectionMode,

    mapStyle?: google.maps.MapTypeStyle[],
    mapOptions?: google.maps.MapOptions,
    markerClustererOptions?: MapClusterOptions,
    labels?: {
        mapTabLabel?: string,
    }
    mapHeight?: string | number,

    centerMarker?: DataMapMarkerProps,

    onDataRefresh?: () => void,
    clearSelectionOnActionExecuted?: boolean
    formMode?: FormMode
}

export const MultiDataMapDefaultProps: Partial<MultiDataMapProps> = {
    dataMapView2: DataMapDefaultDataGridProps,
    dataMapView3: DataMapDefaultDataGridProps,
    dataMapView4: DataMapDefaultDataGridProps,
    dataMapView5: DataMapDefaultDataGridProps,
    labels: {
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
    mapHeight: 'auto',

    enableExport: true,
    selectionMode: 'multi',
    search: [],
    limit: "10000",
    toolbarIconVariant: "light",
    filtersFormSize: "md",
    clearSelectionOnActionExecuted: true
}

export function MultiDataMap(props: MultiDataMapProps) {
    const {
        dataMapView1, dataMapView2, dataMapView3, dataMapView4, dataMapView5,
        labels: dataMapLabels, search, limit, enableExport, autoFocus, toolbarIconVariant, toolbarSize, filtersFormSize, selectionMode,
        mapOptions, mapStyle, mapHeight, centerMarker, clearSelectionOnActionExecuted, formMode
    } = useComponentDefaultProps('MultiDataMap', MultiDataMapDefaultProps, props);

    const theme = useMantineTheme();

    // Map
    const googleMapsAPI = useGoogleMapsAPI();

    // infoWindow
    const infoWindowContentRef = useRef(document.createElement('div'));
    const [infoWindowContent, setInfoWindowContent] = useState<React.ReactNode>(null);

    const { mapReady, placesReady, geocoderReady } = googleMapsAPI;

    const currentMapOptionsRef = useRef<MapOptions>({ ...mapOptions, styles: mapStyle });

    // Multidatamap selected markers
    const [selectedMarkers, setSelectedMarkers] = useState<SelectedMarkerProps>({});

    // Common grid properties

    //toolbar
    const [searchData, setSearchData] = useState<SelectItem[]>(search?.map(s => { return { value: s, label: s } }) as SelectItem[]);

    // TODO: add support for setInitialFiltersFromColumns
    const viewState = useViewState(search, limit);

    // Grids
    // 

    const data1 = useMultiDataMapGrid({ dataMapView: dataMapView1, viewState, mapReady, geocoderReady, placesReady, clearSelectionOnActionExecuted });
    const data2 = useMultiDataMapGrid({ dataMapView: dataMapView2!, viewState, mapReady, geocoderReady, placesReady, clearSelectionOnActionExecuted });
    const data3 = useMultiDataMapGrid({ dataMapView: dataMapView3!, viewState, mapReady, geocoderReady, placesReady, clearSelectionOnActionExecuted });
    const data4 = useMultiDataMapGrid({ dataMapView: dataMapView4!, viewState, mapReady, geocoderReady, placesReady, clearSelectionOnActionExecuted });
    const data5 = useMultiDataMapGrid({ dataMapView: dataMapView5!, viewState, mapReady, geocoderReady, placesReady, clearSelectionOnActionExecuted });

    const IconData1 = data1.dgProps.tabIcon ?? data1.dgProps.entity?.Icon;
    const IconData2 = data2.dgProps.tabIcon ?? data2.dgProps.entity?.Icon;
    const IconData3 = data3.dgProps.tabIcon ?? data3.dgProps.entity?.Icon;
    const IconData4 = data4.dgProps.tabIcon ?? data4.dgProps.entity?.Icon;
    const IconData5 = data5.dgProps.tabIcon ?? data5.dgProps.entity?.Icon;

    const TN = {
        map: 'mapTab', mapIcon: <IconMap size="1rem" />,
        tab1: 'mapTab1', tab1Icon: IconData1 ? <IconData1 size="1rem" /> : null,
        tab2: 'mapTab2', tab2Icon: IconData2 ? <IconData2 size="1rem" /> : null,
        tab3: 'mapTab3', tab3Icon: IconData3 ? <IconData3 size="1rem" /> : null,
        tab4: 'mapTab4', tab4Icon: IconData4 ? <IconData4 size="1rem" /> : null,
        tab5: 'mapTab5', tab5Icon: IconData5 ? <IconData5 size="1rem" /> : null,
    };


    //

    const handleExport = useCallback(() => {
        if (data1.dgProps.entity) {
            data1.dataGridAPI.handleExport();
        }
        if (data2.dgProps.entity) {
            data2.dataGridAPI.handleExport();
        }
        if (data3.dgProps.entity) {
            data3.dataGridAPI.handleExport();
        }
        if (data4.dgProps.entity) {
            data4.dataGridAPI.handleExport();
        }
        if (data5.dgProps.entity) {
            data5.dataGridAPI.handleExport();
        }
    }, [data1.dataGridAPI, data1.dgProps.entity, data2.dataGridAPI, data2.dgProps.entity, data3.dataGridAPI, data3.dgProps.entity, data4.dataGridAPI, data4.dgProps.entity, data5.dataGridAPI, data5.dgProps.entity]);

    const handleRefresh = useCallback(() => {
        viewState.setRefresh((prev) => !prev);
    }, [viewState]);

    const handleToggleSelectable = useCallback(() => {
        if (data1.dgProps.entity) {
            data1.dataGridAPI.handleToggleSelectable();
        }
        if (data2.dgProps.entity) {
            data2.dataGridAPI.handleToggleSelectable();
        }
        if (data3.dgProps.entity) {
            data3.dataGridAPI.handleToggleSelectable();
        }
        if (data4.dgProps.entity) {
            data4.dataGridAPI.handleToggleSelectable();
        }
        if (data5.dgProps.entity) {
            data5.dataGridAPI.handleToggleSelectable();
        }
    }, [
        data1.dataGridAPI,
        data1.dgProps.entity,
        data2.dataGridAPI,
        data2.dgProps.entity,
        data3.dataGridAPI,
        data3.dgProps.entity,
        data4.dataGridAPI,
        data4.dgProps.entity,
        data5.dataGridAPI,
        data5.dgProps.entity,
    ]);


    // Effect for currentMapOptions
    useEffect(() => {
        currentMapOptionsRef.current = { ...mapOptions, styles: mapStyle };
    }, [mapOptions, mapStyle, centerMarker]);

    const getMarkerOptions = useCreateMarkerOptions();

    // Join Map Makers from Data1, 2, 3, 4, 5
    const [markers, setMarkers] = useState<MarkerProps[]>([]);
    useEffect(() => {
        const markers1 = data1.markers;
        const markers2 = data2.markers;
        const markers3 = data3.markers;
        const markers4 = data4.markers;
        const markers5 = data5.markers;

        const centerMarkerMarker: MarkerProps[] = [];

        if (centerMarker) {
            const optionsResult = getMarkerOptions(centerMarker);
            const markerOptions = optionsResult.markerOptions;
            const selectedMarkerOptions = optionsResult.selectedMarkerOptions;
            centerMarkerMarker.push({ markerOptions, selectedMarkerOptions });
            currentMapOptionsRef.current.center = markerOptions.position;
        }
        else if (data1.centerMarker) {
            currentMapOptionsRef.current.center = data1.centerMarker.markerOptions.position;
        }
        else if (data2.centerMarker) {
            currentMapOptionsRef.current.center = data2.centerMarker.markerOptions.position;
        }
        else if (data3.centerMarker) {
            currentMapOptionsRef.current.center = data3.centerMarker.markerOptions.position;
        }
        else if (data4.centerMarker) {
            currentMapOptionsRef.current.center = data4.centerMarker.markerOptions.position;
        }
        else if (data5.centerMarker) {
            currentMapOptionsRef.current.center = data5.centerMarker.markerOptions.position;
        }

        if (!currentMapOptionsRef.current.center) currentMapOptionsRef.current.center = DEFAULT_MAP_CENTER;

        setMarkers([...markers1, ...markers2, ...markers3, ...markers4, ...markers5, ...centerMarkerMarker]);

    }, [centerMarker, data1.markers, data2.markers, data3.markers, data4.markers, data5.markers])


    useEffect(() => {
        const selectedMarkers1 = data1.selectedMarkers;
        const selectedMarkers2 = data2.selectedMarkers;
        const selectedMarkers3 = data3.selectedMarkers;
        const selectedMarkers4 = data4.selectedMarkers;
        const selectedMarkers5 = data5.selectedMarkers;

        setSelectedMarkers({ ...selectedMarkers1, ...selectedMarkers2, ...selectedMarkers3, ...selectedMarkers4, ...selectedMarkers5 })
    }, [data1.selectedMarkers, data2.selectedMarkers, data3.selectedMarkers, data4.selectedMarkers, data5.selectedMarkers]);


    return (
        <>
            <DataGridToolbar
                enableExport={enableExport}
                onExportClick={handleExport}

                onRefreshClick={handleRefresh}
                onCheckboxToggle={handleToggleSelectable}
                autoFocus={autoFocus}
                toolbarIconVariant={toolbarIconVariant}
                size={toolbarSize}

                searchText={viewState.searchText}
                setSearchText={viewState.setSearchText}
                searchData={searchData}
                setSearchData={setSearchData}

                client={dataMapView1?.entity?.API.client}

                FiltersEntity={(dataMapView1?.entity && dataMapView1?.viewName) ? dataMapView1.entity.def.views[dataMapView1.viewName].FiltersEntity : undefined}

                filterValues={viewState.filterValues}
                setFilterValues={viewState.setFilterValues}

                filtersDescription={viewState.filtersDescription}
                setFiltersDescription={viewState.setFiltersDescription}

                filtersFormSize={filtersFormSize!}
            />
            <Tabs pt="xs" defaultValue={TN.map}>
                <Tabs.List>
                    <Tabs.Tab value={TN.map} icon={TN.mapIcon} >{dataMapLabels?.mapTabLabel}</Tabs.Tab>
                    {data1.dgProps.entity &&
                        <Tabs.Tab value={TN.tab1} icon={TN.tab1Icon} >{data1.dgProps.tabLabel ?? data1.dgProps.entity.Title}</Tabs.Tab>
                    }
                    {data2.dgProps.entity &&
                        <Tabs.Tab value={TN.tab2} icon={TN.tab2Icon} >{data2.dgProps.tabLabel ?? data2.dgProps.entity.Title}</Tabs.Tab>
                    }
                    {data3.dgProps.entity &&
                        <Tabs.Tab value={TN.tab3} icon={TN.tab3Icon} >{data3.dgProps.tabLabel ?? data3.dgProps.entity.Title}</Tabs.Tab>
                    }
                    {data4.dgProps.entity &&
                        <Tabs.Tab value={TN.tab4} icon={TN.tab4Icon} >{data4.dgProps.tabLabel ?? data4.dgProps.entity.Title}</Tabs.Tab>
                    }
                    {data5.dgProps.entity &&
                        <Tabs.Tab value={TN.tab5} icon={TN.tab5Icon} >{data5.dgProps.tabLabel ?? data5.dgProps.entity.Title}</Tabs.Tab>
                    }
                </Tabs.List>
                {data1.dgProps.entity &&
                    <Tabs.Panel value={TN.tab1} pt="xs">
                        <DataMapGrid
                            {...data1.dgProps}
                            columnsOverrides={data1.actualColumnsOverrides}
                            selectedRows={data1.selectedRows}
                            setSelectedRows={data1.setSelectedRows}
                            executeViewState={data1.executeViewState}
                            dataGridAPI={data1.dataGridAPI}
                            viewState={data1.viewState}
                            gridHeight={mapHeight}
                            selectionMode={selectionMode!}
                            key={`dmg${TN.tab1}`}
                            renderOnlyWhenVisible={false}
                            formMode={formMode}
                        />
                    </Tabs.Panel>
                }
                {data2.dgProps.entity &&
                    <Tabs.Panel value={TN.tab2} pt="xs">
                        <DataMapGrid
                            {...data2.dgProps}
                            columnsOverrides={data2.actualColumnsOverrides}
                            selectedRows={data2.selectedRows}
                            setSelectedRows={data2.setSelectedRows}
                            executeViewState={data2.executeViewState}
                            dataGridAPI={data2.dataGridAPI}
                            viewState={data2.viewState}
                            gridHeight={mapHeight}
                            selectionMode={selectionMode!}
                            key={`dmg${TN.tab2}`}
                            renderOnlyWhenVisible={false}
                            formMode={formMode}
                        />
                    </Tabs.Panel>
                }
                {data3.dgProps.entity &&
                    <Tabs.Panel value={TN.tab3} pt="xs">
                        <DataMapGrid
                            {...data3.dgProps}
                            columnsOverrides={data3.actualColumnsOverrides}
                            selectedRows={data3.selectedRows}
                            setSelectedRows={data3.setSelectedRows}
                            executeViewState={data3.executeViewState}
                            dataGridAPI={data3.dataGridAPI}
                            viewState={data3.viewState}
                            gridHeight={mapHeight}
                            selectionMode={selectionMode!}
                            key={`dmg${TN.tab3}`}
                            renderOnlyWhenVisible={false}
                            formMode={formMode}
                        />
                    </Tabs.Panel>
                }
                {data4.dgProps.entity &&
                    <Tabs.Panel value={TN.tab4} pt="xs">
                        <DataMapGrid
                            {...data4.dgProps}
                            columnsOverrides={data4.actualColumnsOverrides}
                            selectedRows={data4.selectedRows}
                            setSelectedRows={data4.setSelectedRows}
                            executeViewState={data4.executeViewState}
                            dataGridAPI={data4.dataGridAPI}
                            viewState={data4.viewState}
                            gridHeight={mapHeight}
                            selectionMode={selectionMode!}
                            key={`dmg${TN.tab4}`}
                            renderOnlyWhenVisible={false}
                            formMode={formMode}
                        />
                    </Tabs.Panel>
                }
                {data5.dgProps.entity &&
                    <Tabs.Panel value={TN.tab5} pt="xs">
                        <DataMapGrid
                            {...data5.dgProps}
                            columnsOverrides={data5.actualColumnsOverrides}
                            selectedRows={data5.selectedRows}
                            setSelectedRows={data5.setSelectedRows}
                            executeViewState={data5.executeViewState}
                            dataGridAPI={data5.dataGridAPI}
                            viewState={data5.viewState}
                            gridHeight={mapHeight}
                            selectionMode={selectionMode!}
                            key={`dmg${TN.tab5}`}
                            renderOnlyWhenVisible={false}
                            formMode={formMode}
                        />
                    </Tabs.Panel>
                }
                <Tabs.Panel value={TN.map} pt='xs'>
                    <Stack spacing="sm">
                        <MultiDataMapActionsToolbar
                            data1={data1}
                            data2={data2}
                            data3={data3}
                            data4={data4}
                            data5={data5}
                            parentFormMode={formMode}
                        />
                        {mapReady && placesReady && geocoderReady &&
                            <>
                                {((data1.dataGridAPI.isLoading || data1.executeViewState.loading) && markers.length === 0) &&
                                    <FakeProgressBar size="xs" />
                                }
                                <GoogleMapsInfoWindow container={infoWindowContentRef.current} infoWindowContent={infoWindowContent} />
                                <GoogleMapCluster
                                    style={{ height: mapHeight === 'auto' ? '50vh' : mapHeight }}
                                    mapOptions={currentMapOptionsRef.current}
                                    infoWindowContentRef={infoWindowContentRef}
                                    setInfoWindowContent={setInfoWindowContent}
                                    markers={markers}
                                    selectedMarkers={selectedMarkers}
                                />

                            </>
                        }
                    </Stack>
                </Tabs.Panel>
            </Tabs>

        </>
    )
}