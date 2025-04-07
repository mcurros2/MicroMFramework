import { MantineNumberSize, Skeleton, Stack, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { AlertError, AlertInfo, latLng } from "../Core";
import { AddressSearchDefaultZoomLevels, DEFAULT_MAP_CENTER, GeocodeOperationResult, GoogleMap, GoogleMapsErrorStatus, GoogleMarker, MapOptions, MappingAddressType, MarkerOptions } from "../GoogleMaps";

export interface AddressMapMarkerPosition {
    position: latLng,
    mappedAddressType?: MappingAddressType
}

export interface AddressMapMarkerProps {
    initialSearchAddress?: string,
    readOnly?: boolean,
    markerPosition?: AddressMapMarkerPosition,
    defaultMapCenter?: latLng,
    onMarkerChanged?: (postion: latLng, geocodeOperationResult?: google.maps.GeocoderResult) => void,
    onAPIError?: (status: GoogleMapsErrorStatus) => void,

    errorTitle?: string,
    errorMessage?: string,
    markerInfoTitle?: string,
    markerInfoMessage?: string,
    mapHeight?: MantineNumberSize,
    showErrors?: boolean,

    draggable?: boolean,
    initialMapZoomLevel?: number,

    fullScreenControl?: boolean,
}

export const AddressMapMarkerDefaultProps: Partial<AddressMapMarkerProps> = {
    defaultMapCenter: DEFAULT_MAP_CENTER,
    mapHeight: "50vh",
    errorTitle: "google maps error",
    errorMessage: "An error occurred with google maps API:",
    markerInfoTitle: "Marker location changed",
    markerInfoMessage: "The marker has been moved to a new location. Result for marker location:",
    draggable: true,
    initialMapZoomLevel: AddressSearchDefaultZoomLevels.address,
    fullScreenControl: false,
}

export function AddressMapMarker(props: AddressMapMarkerProps) {
    const googleMapsAPI = useGoogleMapsAPI();

    const { mapReady, placesReady, geocoderReady } = googleMapsAPI;

    const addressProps = useComponentDefaultProps('AddressMapMarker', AddressMapMarkerDefaultProps, props);

    return (
        <>
            {mapReady && placesReady && geocoderReady ?
                (<AddressMapMarkerInternal {...addressProps} {...googleMapsAPI} />)
                :
                (<Skeleton height={props.mapHeight} />)
            }
        </>
    )
}
function AddressMapMarkerInternal(props: AddressMapMarkerProps & ReturnType<typeof useGoogleMapsAPI>) {

    const {
        markerPosition, defaultMapCenter, readOnly, errorMessage, errorTitle, initialSearchAddress, markerInfoTitle, markerInfoMessage,
        onAPIError, onMarkerChanged, geocode, mapHeight, showErrors, draggable, initialMapZoomLevel, fullScreenControl
    } = props;

    const [errorStatus, setErrorStatus] = useState<GoogleMapsErrorStatus | undefined>();

    const [draggedFormattedAddress, setDraggedFormattedAddress] = useState<string | undefined>(undefined);
    const [markerActualPos, setMarkerActualPos] = useState<latLng | undefined>(markerPosition?.position);
    const [markerPanZoom, setMarkerPanZoom] = useState<number | undefined>(AddressSearchDefaultZoomLevels.address);

    const mapOptions = useMemo<MapOptions>(() => ({
        zoom: markerActualPos ? markerPanZoom : initialMapZoomLevel,
        fullscreenControl: fullScreenControl,
        gestureHandling: 'greedy',
        clickableIcons: false,
        center: (markerActualPos && markerActualPos.lat !== null && markerActualPos.lng !== null) ? markerActualPos : defaultMapCenter
    }), [defaultMapCenter, initialMapZoomLevel, markerActualPos, markerPanZoom, fullScreenControl]);

    const addressMarkerOptions = useMemo<MarkerOptions>(() => ({
        position: (markerActualPos && markerActualPos.lat !== null && markerActualPos.lng !== null) ? markerActualPos : defaultMapCenter,
        visible: !!markerActualPos,
        animation: google.maps.Animation.DROP,
        draggable: readOnly ? false : draggable,
    }), [defaultMapCenter, markerActualPos, readOnly, draggable]);

    useEffect(() => {
        setMarkerActualPos(markerPosition?.position);
        setMarkerPanZoom(markerPosition?.mappedAddressType ? AddressSearchDefaultZoomLevels[markerPosition.mappedAddressType || 'unknown'] : initialMapZoomLevel);
    }, [initialMapZoomLevel, markerPosition]);

    const handleDragEnd = useCallback((position: google.maps.LatLng) => {
        const latLng = { lat: position.lat(), lng: position.lng() };

        setMarkerActualPos(latLng);

        geocode({ location: position }, (results, status) => {
            const operationResult: GeocodeOperationResult = {};
            operationResult.status = status || 'UNKNOWN';

            if (status === google.maps.GeocoderStatus.OK) {
                const filteredResults = results?.filter(result => result.types.includes('route'));
                operationResult.result = (filteredResults && filteredResults.length > 0) ? filteredResults[0] : results ? results[0] : undefined;
                setDraggedFormattedAddress(operationResult.result?.formatted_address);
            } else if (status === google.maps.GeocoderStatus.ZERO_RESULTS) {
                // MMC: on empty results we set the queryStatus to OK to clear the marker address
                operationResult.status = "OK";
            } else {
                const errorStatus: GoogleMapsErrorStatus = { status: status || 'UNKNOWN', origin: "Geocoder" };
                setErrorStatus(errorStatus);
                if (onAPIError) onAPIError(errorStatus);
            }

            if (onMarkerChanged) {
                onMarkerChanged(latLng, operationResult.result);
            }

        });

    }, [geocode, onAPIError, onMarkerChanged]);

    return (
        <>
            <GoogleMap style={{ height: mapHeight, resize: "vertical" }} mapOptions={mapOptions}>
                <GoogleMarker key="address-marker" markerOptions={addressMarkerOptions} onDragEnd={handleDragEnd} />
            </GoogleMap>
            {(draggedFormattedAddress && draggedFormattedAddress !== initialSearchAddress) &&
                <AlertInfo title={markerInfoTitle}>
                    <Stack>
                        {markerInfoMessage} {draggedFormattedAddress}
                    </Stack>
                </AlertInfo>
            }
            {showErrors && errorStatus && <AlertError title={errorTitle}>{errorMessage} {errorStatus.status} ({errorStatus.origin})</AlertError>}
        </>)
}