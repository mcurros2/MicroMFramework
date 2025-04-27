import { ActionIcon, Card, Group, Loader, MantineNumberSize, Skeleton, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { IconX } from "@tabler/icons-react";
import { useCallback, useMemo, useState } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { AlertError, latLng } from "../Core";
import { GoogleAddressAutocomplete, GoogleAddressAutocompleteRestrictions, OnAddressFoundCallback } from "../GoogleMaps/GoogleAddressAutocomplete";
import { GoogleMapRegionSelectorMap } from "../GoogleMaps/GoogleMapRegionSelectorMap";
import { ClickedRegionData, GoogleMapRegionSelectorProps, SelectedRegionData } from "../GoogleMaps/GoogleMapRegionSelectorTypes";
import { GoogleMarker } from "../GoogleMaps/GoogleMarker";
import { DEFAULT_MAP_CENTER, GoogleMapsErrorStatus, MapOptions } from "../GoogleMaps/Mapping.types";
import { RegionSelectorItem } from "./RegionSelectorItem";


export interface RegionSelectorProps extends Omit<GoogleMapRegionSelectorProps, 'googleMapsAPI'> {
    mapId: string,
    readOnly?: boolean,
    mapRestrictions?: google.maps.LatLngBoundsLiteral[],
    defaultMapCenter?: latLng,
    onAPIError?: (status: GoogleMapsErrorStatus) => void,
    errorTitle?: string,
    errorMessage?: string,
    mapHeight?: MantineNumberSize,
    showErrors?: boolean,
    searchLabel?: string,
    loadingLabel?: string,
    unknownStateLabel?: string,
    regionListMode?: 'count' | 'names',
    suppressCountyString?: string,
    searchDescriptionLabel?: string,
    mapHelpLabel?: string,
    selectedRegionsLabel?: string,
    autoFocus?: boolean,
}

export const RegionSelectorDefaultProps: Partial<RegionSelectorProps> = {
    defaultMapCenter: DEFAULT_MAP_CENTER,
    mapHeight: "50vh",
    errorTitle: "Google maps error",
    errorMessage: "An error occurred with google maps API:",
    mapCenter: DEFAULT_MAP_CENTER,
    searchLabel: "Search",
    loadingLabel: "Loading",
    unknownStateLabel: "Unknown state",
    regionListMode: 'names',
    suppressCountyString: 'county',
    searchDescriptionLabel: "Search for a region, place or address and click on the map to select regions",
    mapHelpLabel: "* Use the zoom to select States, Counties or Zip codes. Click over the region to select/deselect",
    selectedRegionsLabel: "Selected Regions",
    maxRegions: Infinity
}

export function RegionSelector(props: RegionSelectorProps) {
    const { mapReady, placesReady, geocoderReady } = useGoogleMapsAPI();

    const selectorProps = useComponentDefaultProps('RegionSelector', RegionSelectorDefaultProps, props);

    return (
        <>
            {mapReady && placesReady && geocoderReady ?
                (<RegionSelectorInternal {...selectorProps} />)
                :
                (<Skeleton height="50vh" />)
            }
        </>
    )
}

function RegionSelectorInternal(props: RegionSelectorProps) {
    const {
        mapHeight, mapCenter, mapId, readOnly, showErrors, errorTitle, errorMessage,
        countries, searchLabel, onAPIError, onRegionClicked, onSelectionChanged, selectedRegions,
        setSelectedRegions, loadingLabel, unknownStateLabel, regionListMode, suppressCountyString,
        searchDescriptionLabel, mapHelpLabel, clickedRegions, setClickedRegions, selectedRegionsLabel,
        autoFocus,
        ...regionSelectorProps
    } = props;

    const [markerActualPos, setMarkerActualPos] = useState<latLng | undefined>();
    const [errorStatus, setErrorStatus] = useState<GoogleMapsErrorStatus | undefined>();
    const [internalMapCenter, setInternalMapCenter] = useState<latLng>(mapCenter || DEFAULT_MAP_CENTER);
    const [internalMapZoom, setInternalMapZoom] = useState<number>(5);

    const mapOptions = useMemo<MapOptions>(() => ({
        zoom: internalMapZoom,
        center: internalMapCenter,
        mapId: mapId,
        streetViewControl: false,
        fullscreenControl: false,
        mapTypeControl: false,
        gestureHandling: 'greedy',
        //clickableIcons: false,
    }), [internalMapCenter, internalMapZoom, mapId]);

    const addressMarkerOptions = useMemo(() => ({
        position: markerActualPos,
        visible: !!markerActualPos,
        animation: google.maps.Animation.DROP,
        draggable: false,
    }), [markerActualPos]);

    // Autocomplete
    const autocompleteRestrictions = useMemo<GoogleAddressAutocompleteRestrictions>(() => ({
        country: countries || []
    }), [countries]);

    const handleAddressFound = useCallback<OnAddressFoundCallback>(found => {
        if (found.place.geometry?.location) {
            const currentPos = { lat: found.place.geometry.location.lat(), lng: found.place.geometry.location.lng() };
            setMarkerActualPos(currentPos);
            setInternalMapCenter(currentPos);
            setInternalMapZoom(11);
        } else {
            setMarkerActualPos(undefined);
        }
    }, []);

    const handleAutocompleteError = useCallback((status: string) => {
        const apiError: GoogleMapsErrorStatus = { status: status, origin: "Places" };
        setErrorStatus(apiError);
        if (onAPIError) onAPIError(apiError);
    }, [onAPIError]);

    // Region selector
    const handleRegionClicked = useCallback((place_id: string) => {
        setMarkerActualPos(undefined);
    }, []);

    const selectorProps = useMemo<Omit<GoogleMapRegionSelectorProps, 'googleMapsAPI'>>(() => ({
        ...regionSelectorProps,
        countries,
        selectedRegions,
        setSelectedRegions,
        clickedRegions,
        setClickedRegions,
        onSelectionChanged: (selected: Record<string, SelectedRegionData>) => {
            if (onSelectionChanged) onSelectionChanged(selected);
        },
        onRegionClicked: (place_id) => {
            handleRegionClicked(place_id); if (onRegionClicked) onRegionClicked(place_id);
        }
    }), [clickedRegions, countries, handleRegionClicked, onRegionClicked, onSelectionChanged, regionSelectorProps, selectedRegions, setClickedRegions, setSelectedRegions]);

    // Grouped regions
    const groupedRegions = useMemo(() => {
        const groups = new Map<string, {
            countryId?: string;
            stateId: string;
            stateName?: string;
            counties: Set<string>;
            postalCodes: Set<string>;
            regions: SelectedRegionData[];
        }>();

        Object.values(selectedRegions).forEach((region) => {

            const stateId = region.administrativeAreaLevel1Id || 'UNKNOWN_STATE';

            const group_key = `${region.countryId}-${stateId}`;
            let group = groups.get(group_key);

            if (!group) {
                group = {
                    countryId: region.countryId,
                    stateId: stateId,
                    stateName: region.administrativeAreaLevel1Name,
                    counties: new Set<string>(),
                    postalCodes: new Set<string>(),
                    regions: [],
                };
                groups.set(group_key, group);
            }

            if (region.featureType === google.maps.FeatureType.ADMINISTRATIVE_AREA_LEVEL_2) {
                group.counties.add(region.administrativeAreaLevel2Id || 'UNKNOWN_COUNTY');
            }

            if (region.featureType === google.maps.FeatureType.POSTAL_CODE) {
                group.postalCodes.add(region.postalCode || 'UNKNOWN_POSTAL_CODE');
            }

            group.regions.push(region);
        });

        return Array.from(groups.values());
    }, [selectedRegions]);


    // Delete group
    const handleDeleteGroup = useCallback((groupRegions: SelectedRegionData[]) => {
        setClickedRegions((prev) => {
            const updated: Record<string, ClickedRegionData> = { ...prev };
            groupRegions.forEach(region => {
                delete updated[region.placeId];
            });
            return updated;
        });
    }, [setClickedRegions]);

    return (
        <Stack spacing="md">
            <GoogleAddressAutocomplete
                label={searchLabel}
                restrictions={autocompleteRestrictions}
                onAddressFound={handleAddressFound}
                readOnly={readOnly}
                onAPIError={handleAutocompleteError}
                description={searchDescriptionLabel}
                data-autofocus={autoFocus}
                autoFocus={autoFocus}
            />
            <Stack spacing="0.2rem">
                <Text size="xs" weight={600} color="dimmed">{mapHelpLabel}</Text>
                <GoogleMapRegionSelectorMap style={{ height: mapHeight, resize: "vertical", flexGrow: 1 }} mapOptions={mapOptions} regionSelectorProps={selectorProps}>
                    <GoogleMarker key="address-marker" markerOptions={addressMarkerOptions} />
                </GoogleMapRegionSelectorMap>
            </Stack>
            <Card withBorder>
                <Stack spacing="xs">
                    <Group>
                        {// check to see if any selected regions are loading and show a loader
                            (Object.keys(clickedRegions).length !== Object.keys(selectedRegions).length) && <Loader size="xs" />
                        }
                        <Text size="sm">{selectedRegionsLabel}</Text>
                        <ActionIcon size="xs" radius="xl" variant="transparent" onClick={() => { setClickedRegions({}); setSelectedRegions({}); }}>
                            <IconX size="0.75rem" />
                        </ActionIcon>
                    </Group>
                    <Group spacing="xs">
                        {
                            groupedRegions.map((group) => (
                                <RegionSelectorItem
                                    key={`group-${group.countryId}-${group.stateId}`}
                                    onCloseClick={() => handleDeleteGroup(group.regions)}
                                >
                                    {
                                        [
                                            group.stateName === undefined ? loadingLabel : group.stateName || unknownStateLabel,
                                            group.counties.size > 0 ? (
                                                regionListMode === 'names'
                                                    ? `counties: [${Array.from(group.counties)
                                                        .map(county => county.replace(new RegExp(`\\s*${suppressCountyString}\\s*`, 'i'), ''))
                                                        .join(', ')}]`
                                                    : `counties: ${group.counties.size}`
                                            ) : null,
                                            group.postalCodes.size > 0 ? (
                                                regionListMode === 'names'
                                                    ? `zipcodes: [${Array.from(group.postalCodes).join(', ')}]`
                                                    : `zipcodes: ${group.postalCodes.size}`
                                            ) : null,
                                            group.countryId,
                                        ].filter(Boolean).join(', ')
                                    }
                                </RegionSelectorItem>
                            ))
                        }
                    </Group>
                </Stack>
            </Card>
            {showErrors && errorStatus && <AlertError title={errorTitle}>{errorMessage} {errorStatus.status} ({errorStatus.origin})</AlertError>}
        </Stack>
    )
}
