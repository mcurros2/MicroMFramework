import { Skeleton, Stack, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo, useState } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { AlertError } from "../Core";
import { AddressFoundResult, GoogleAddressAutocomplete, GoogleAddressAutocompleteRestrictions, OnAddressFoundCallback } from "../GoogleMaps";
import { AddressMappingRule, GoogleMapsErrorStatus } from "../GoogleMaps/Mapping.types";
import { AddressMapMarker, AddressMapMarkerPosition, AddressMapMarkerProps } from "./AddressMapMarker";


export interface AddressSearchProps extends Omit<AddressMapMarkerProps, 'showErrors'> {
    countries: string[],
    onAddressFound?: (result: AddressFoundResult) => void,
    mappingRules?: AddressMappingRule[]
    addressLabel?: string,
    draggable?: boolean,
}

export const AddressSearchDefaultProps: Partial<AddressSearchProps> = {
    addressLabel: "Address",
    mappingRules: [],
    draggable: true
}

export function AddressSearch(props: AddressSearchProps) {
    const googleMapsAPI = useGoogleMapsAPI();

    const { mapReady, placesReady, geocoderReady } = googleMapsAPI;

    const addressProps = useComponentDefaultProps('AddressSearch', AddressSearchDefaultProps, props);

    return (
        <>
            {mapReady && placesReady && geocoderReady ?
                (<AddressSearchInternal {...addressProps} {...googleMapsAPI} />)
                :
                (<Skeleton height="50vh" />)
            }
        </>
    )
}

function AddressSearchInternal(props: AddressSearchProps & ReturnType<typeof useGoogleMapsAPI>) {
    const {
        addressLabel, onMarkerChanged, defaultMapCenter, markerPosition, errorMessage,
        errorTitle, onAPIError, readOnly, countries, initialSearchAddress, onAddressFound, mappingRules,
        draggable,
        ...rest
    } = props;

    const [errorStatus, setErrorStatus] = useState<GoogleMapsErrorStatus | undefined>();

    const [lastFormattedAddress, setLastFormattedAddress] = useState<string | undefined>(initialSearchAddress);
    const [markerActualPos, setMarkerActualPos] = useState<AddressMapMarkerPosition | undefined>(markerPosition);

    const autocompleteRestrictions = useMemo<GoogleAddressAutocompleteRestrictions>(() => ({
        country: countries
    }), [countries]);

    const handleAddressFound = useCallback<OnAddressFoundCallback>(found => {

        // MMC: when address changes, the marker gets reset to the same position as the address
        if (found.place.geometry?.location) {
            const currentPos = { lat: found.place.geometry.location.lat(), lng: found.place.geometry.location.lng() };

            setMarkerActualPos({position: currentPos, mappedAddressType: found.address?.mappedAddressType});
            setLastFormattedAddress(found.suggestionDescription || found.place.formatted_address);
        }
        else {
            setMarkerActualPos(undefined);
            setLastFormattedAddress(undefined);
        }

        if (onAddressFound) {
            onAddressFound(found);
        }

    }, [onAddressFound]);

    const handleAutocompleteError = useCallback((status: string) => {
        const apiError: GoogleMapsErrorStatus = { status: status, origin: "Places" };
        setErrorStatus(apiError);
        if (onAPIError) onAPIError(apiError);
    }, [onAPIError]);

    const handleMarkerError = useCallback((status: GoogleMapsErrorStatus) => {
        setErrorStatus(status);
        if (onAPIError) onAPIError(status);
    }, [onAPIError]);


    return (
        <Stack>
            <GoogleAddressAutocomplete
                label={addressLabel}
                restrictions={autocompleteRestrictions}
                onAddressFound={handleAddressFound}
                readOnly={readOnly}
                value={initialSearchAddress}
                onAPIError={handleAutocompleteError}
                mappingRules={mappingRules}
            />
            <AddressMapMarker
                {...rest}
                initialSearchAddress={lastFormattedAddress}
                markerPosition={markerActualPos}
                defaultMapCenter={defaultMapCenter}
                onMarkerChanged={onMarkerChanged}
                onAPIError={handleMarkerError}
                readOnly={readOnly}
                showErrors={false}
                draggable={draggable}
            />
            {errorStatus && <AlertError title={errorTitle}>{errorMessage} {errorStatus.status} ({errorStatus.origin})</AlertError>}
        </Stack>
    );
}