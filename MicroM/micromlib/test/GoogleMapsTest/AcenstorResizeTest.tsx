import { Box, ScrollArea, Stack, TextInput } from "@mantine/core";
import { useGoogleGeocoderAPIloader, useGoogleMapsAPILoader, useGooglePlacesAPIloader } from "GoogleMapsAPI/GoogleMapsAPILoader";
import { GoogleAddressAutocomplete, GoogleAddressAutocompleteRestrictions, GoogleMap, GoogleMarker, MapOptions, MarkerOptions, OnAddressFoundCallback, OnDragEndCallback } from "UI/GoogleMaps";
import { useCallback, useMemo, useRef, useState } from "react";
import { useAncestorResize } from "../../src";


export function AncestorResizeTest() {
    const [mapReady] = useGoogleMapsAPILoader();
    const [placesReady] = useGooglePlacesAPIloader();
    const [geocoderReady] = useGoogleGeocoderAPIloader();

    return mapReady && placesReady && geocoderReady && <AddressAutocompleteTestInternal></AddressAutocompleteTestInternal>
}
function AddressAutocompleteTestInternal() {
    const [addressPos, setAddressPos] = useState<google.maps.LatLng>();

    const geocoderServiceRef = useRef<google.maps.Geocoder>(new google.maps.Geocoder());
    const [geocodeResultText, setGeocodeResultText] = useState<string>();

    const autocompleteRestrictions = useMemo<GoogleAddressAutocompleteRestrictions>(() => ({
        country: ["us", "ca"]
    }), []);

    const mapOptions = useMemo<MapOptions>(() => ({
        zoom: addressPos ? 17 : 10,
        fullscreenControl: false,
        center: addressPos || { lat: -34.603683, lng: -58.381557 }
    }), [addressPos]);

    const markerOptions = useMemo<MarkerOptions>(() => ({
        position: addressPos,
        visible: !!addressPos,
        draggable: true,
        //clickable: true,
        animation: google.maps.Animation.DROP
    }), [addressPos]);

    const handleAddressFound = useCallback<OnAddressFoundCallback>(result => {
        console.debug("Address found", result);
        setAddressPos(result.place.geometry?.location);
    }, []);

    const handleDragEnd = useCallback<OnDragEndCallback>((position) => {
        console.debug("Drag end", position);
        if (geocoderServiceRef.current) {
            geocoderServiceRef.current.geocode({ location: position }, (results, status) => {
                if (status === google.maps.GeocoderStatus.OK) {
                    setGeocodeResultText(results ? results[0].formatted_address : 'No results');
                }
                else setGeocodeResultText(`Geocoder failed: ${status}`);
            });
        }
    }, []);

    const resize = useAncestorResize({ width: "20vw", height: "10vh" });

    console.log('ancestor resize', resize.size);

    return <Stack>
        <GoogleAddressAutocomplete autoFocus style={{ marginBottom: "1rem" }} restrictions={autocompleteRestrictions} onAddressFound={handleAddressFound} />
        <ScrollArea h="70vh">
            <Box ref={resize.ref}>
                <GoogleMap style={{ height: resize.size.height || '200px', resize: "vertical" }} mapOptions={mapOptions}>
                    <GoogleMarker markerOptions={markerOptions} onDragEnd={handleDragEnd} />
                </GoogleMap>
            </Box>
        </ScrollArea>
        <TextInput readOnly value={geocodeResultText} />
    </Stack>
}
