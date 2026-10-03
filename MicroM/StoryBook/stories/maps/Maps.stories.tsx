import { Code, Stack, Text } from "@mantine/core";
import {
    AddressAutocomplete, AddressLookup, AddressSearch as MicroMAddressSearch, ARMappingRules, ClickedRegionData, CommonFlags as c, DataMap as MicroMDataMap, DataMapPage as MicroMDataMapPage, DataRegion as MicroMDataRegion,
    DataResult, DefaultColumns, Entity, EntityColumn, EntityDefinition, EntityForm, GoogleMap as MicroMGoogleMap, GoogleMarker, MicroMClient, MultiDataMap as MicroMMultiDataMap,
    RegionSelector as MicroMRegionSelector, SelectedRegionData, useEntityForm, useGoogleAddressMappingRules, useGoogleMapsAPILoader, UYMappingRules
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Branches, Customers } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";
import { GoogleMapsKeyForm, googleMapsMapId, RequiresGoogleMaps } from "./googleMapsConfig";

class AddressDemoDef extends EntityDefinition {
    columns = {
        c_address_id: new EntityColumn<string>({ name: 'c_address_id', type: 'char', length: 20, flags: c.PK, prompt: 'Address id' }),
        vc_address: new EntityColumn<string>({ name: 'vc_address', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Address' }),
        ...DefaultColumns()
    };
    procs = { adr_getRegions: { name: 'adr_getRegions' } };
    constructor() { super('AddressDemo'); }
}

class AddressDemo extends Entity<AddressDemoDef> {
    constructor(client: MicroMClient, parentKeys = {}) { super(client, new AddressDemoDef(), parentKeys); }
}

function useAddressDemo() {
    return useMemo(() => {
        const client = createMockClient();
        client.registerTable({ entityName: "AddressDemo", pk: ["c_address_id"], descriptionColumn: "vc_address", viewColumns: [], rows: [] });
        return new AddressDemo(client);
    }, []);
}

const meta = {
    title: "MicroM/Maps",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const Setup: Story = { render: () => <GoogleMapsKeyForm /> };

const customersMapGrid = (client: MicroMClient) => {
    const entity = new Customers(client);
    return { entity, viewName: entity.def.views.cus_brwMap.name, selectionMode: "multi" as const, formMode: "edit" as const };
};

export const DataMap: Story = {
    name: "DataMap",
    render: () => (
        <RequiresGoogleMaps>
            <MicroMDataMap dataGridProps={customersMapGrid(createMockClient())} latitudRecordIndex={2} longitudRecordIndex={3} mapHeight="65vh" />
        </RequiresGoogleMaps>
    ),
};

export const DataMapPage: Story = {
    name: "DataMapPage",
    render: () => (
        <RequiresGoogleMaps>
            <MicroMDataMapPage client={createMockClient()} entityConstructor={cl => new Customers(cl)} formMode="edit"
                dataGridProps={{ viewName: "cus_brwMap", selectionMode: "multi" }} latitudRecordIndex={2} longitudRecordIndex={3} mapHeight="60vh" />
        </RequiresGoogleMaps>
    ),
};

export const MultiDataMap: Story = {
    name: "MultiDataMap",
    render: () => {
        const client = createMockClient();
        const customers = new Customers(client);
        const branches = new Branches(client);
        return (
            <RequiresGoogleMaps>
                <MicroMMultiDataMap formMode="edit" mapHeight="60vh"
                    dataMapView1={{ entity: customers, viewName: "cus_brwMap", latitudRecordIndex: 2, longitudRecordIndex: 3, dataSetId: "customers", tabLabel: "Customers" }}
                    dataMapView2={{ entity: branches, viewName: "bra_brwStandard", latitudRecordIndex: 2, longitudRecordIndex: 3, dataSetId: "branches", tabLabel: "Branches" }} />
            </RequiresGoogleMaps>
        );
    },
};

function MarkersMap() {
    const [mapReady] = useGoogleMapsAPILoader();
    if (!mapReady) return <Text size="sm">Loading Google Maps...</Text>;
    return (
        <div style={{ height: "60vh" }}>
            <MicroMGoogleMap mapOptions={{ zoom: 5, center: { lat: -33.5, lng: -62.5 } }}>
                <GoogleMarker markerOptions={{ position: { lat: -34.61, lng: -58.38 }, title: "Buenos Aires" }} />
                <GoogleMarker markerOptions={{ position: { lat: -31.42, lng: -64.18 }, title: "Cordoba" }} />
                <GoogleMarker markerOptions={{ position: { lat: -34.90, lng: -56.16 }, title: "Montevideo", draggable: true }} />
            </MicroMGoogleMap>
        </div>
    );
}

export const GoogleMap: Story = { name: "GoogleMap and GoogleMarker", render: () => <RequiresGoogleMaps><MarkersMap /></RequiresGoogleMaps> };

function AddressInputsDemo() {
    const entity = useAddressDemo();
    const entityForm = useEntityForm({ entity, initialFormMode: "add", getDataOnInit: false });
    const { mappingRules, addMappingRule } = useGoogleAddressMappingRules();
    useEffect(() => { addMappingRule([ARMappingRules, UYMappingRules]); }, [addMappingRule]);
    const [found, setFound] = useState<unknown>();
    return (
        <EntityForm formAPI={entityForm}>
            <Stack>
                <AddressAutocomplete entityForm={entityForm} column={entity.def.columns.vc_address} countries={["ar", "uy"]} mappingRules={mappingRules} onAddressFound={setFound} label="AddressAutocomplete" />
                <AddressLookup entityForm={entityForm} column={entity.def.columns.vc_address} countries={["ar", "uy"]} mappingRules={mappingRules} onAddressFound={setFound} label="AddressLookup (opens AddressSearchForm)" />
                <Text size="xs" color="dimmed">Last result: <Code>{found ? JSON.stringify(found).slice(0, 300) : "(none)"}</Code></Text>
            </Stack>
        </EntityForm>
    );
}

export const AddressInputs: Story = { name: "AddressAutocomplete and AddressLookup", render: () => <RequiresGoogleMaps><AddressInputsDemo /></RequiresGoogleMaps> };

export const AddressSearch: Story = {
    name: "AddressSearch",
    render: () => <RequiresGoogleMaps><MicroMAddressSearch countries={["ar", "uy"]} mapHeight="50vh" draggable onAddressFound={r => console.info("Address found", r)} /></RequiresGoogleMaps>,
};

function Regions() {
    const [selectedRegions, setSelectedRegions] = useState<Record<string, SelectedRegionData>>({});
    const [clickedRegions, setClickedRegions] = useState<Record<string, ClickedRegionData>>({});
    return (
        <Stack>
            <Text size="xs" color="dimmed">Map ID: <Code>{googleMapsMapId}</Code> (needs data-driven styling for boundaries)</Text>
            <MicroMRegionSelector mapId={googleMapsMapId} countries={["AR"]} mapCenter={{ lat: -34.6, lng: -58.4 }}
                selectedRegions={selectedRegions} setSelectedRegions={setSelectedRegions} clickedRegions={clickedRegions} setClickedRegions={setClickedRegions} />
            <Text size="xs" color="dimmed">Selected: <Code>{Object.keys(selectedRegions).join(", ") || "(none)"}</Code></Text>
        </Stack>
    );
}

export const RegionSelector: Story = { name: "RegionSelector", render: () => <RequiresGoogleMaps><Regions /></RequiresGoogleMaps> };

function DataRegionStory() {
    const entity = useAddressDemo();
    const [selectedRegions, setSelectedRegions] = useState<Record<string, SelectedRegionData>>({});
    const [clickedRegions, setClickedRegions] = useState<Record<string, ClickedRegionData>>({});
    const parentKeys = useMemo(() => ({}), []);
    const mapDataResult = useCallback((data: DataResult) => Object.fromEntries(
        data.records.map(r => [String(r[0]), { placeId: String(r[0]) } as SelectedRegionData])
    ), []);
    return (
        <Stack>
            <Text size="xs" color="dimmed">
                Loads the saved selection with <Code>proc(adr_getRegions)</Code>; the mock returns no regions, so start selecting on the map.
            </Text>
            <MicroMDataRegion entity={entity} queryName="adr_getRegions" parentKeys={parentKeys} mapDataResult={mapDataResult} mapId={googleMapsMapId} countries={["AR"]}
                selectedRegions={selectedRegions} setSelectedRegions={setSelectedRegions} clickedRegions={clickedRegions} setClickedRegions={setClickedRegions} />
        </Stack>
    );
}

export const DataRegion: Story = { name: "DataRegion", render: () => <RequiresGoogleMaps><DataRegionStory /></RequiresGoogleMaps> };
