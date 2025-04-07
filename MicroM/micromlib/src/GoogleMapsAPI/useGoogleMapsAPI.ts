import { useCallback, useEffect, useRef } from "react";
import { useGoogleGeocoderAPIloader, useGoogleGeometryAPILoader, useGoogleMapsAPILoader, useGooglePlacesAPIloader } from "./GoogleMapsAPILoader";

export function useGoogleMapsAPI() {
    const [mapReady] = useGoogleMapsAPILoader();
    const [placesReady] = useGooglePlacesAPIloader();
    const [geocoderReady] = useGoogleGeocoderAPIloader();
    const [geometryReady] = useGoogleGeometryAPILoader();

    const placesServiceRef = useRef<google.maps.places.PlacesService>()
    const autocompleteServiceRef = useRef<google.maps.places.AutocompleteService>();
    const geocoderServiceRef = useRef<google.maps.Geocoder>();
    const sessionTokenRef = useRef<google.maps.places.AutocompleteSessionToken>()

    const attributionsRef = useRef<HTMLDivElement>(null);

    const refreshSessionToken = useCallback(() => {
        sessionTokenRef.current = new google.maps.places.AutocompleteSessionToken();
    }, []);

    useEffect(() => {
        if (!mapReady || !placesReady || !geocoderReady || !geometryReady) return;

        if (!autocompleteServiceRef.current) {
            autocompleteServiceRef.current = new google.maps.places.AutocompleteService();
            refreshSessionToken();
        }
        if (!placesServiceRef.current && attributionsRef.current) {
            placesServiceRef.current = new google.maps.places.PlacesService(attributionsRef.current);
        }

        if (!geocoderServiceRef.current) {
            geocoderServiceRef.current = new google.maps.Geocoder();
        }


    }, [geocoderReady, geometryReady, mapReady, placesReady, refreshSessionToken]);

    const getAutocompletePredictions = useCallback(
        (props: {
            searchText: string,
            restrictions?: google.maps.places.ComponentRestrictions,
            callback: (predictions: google.maps.places.AutocompletePrediction[] | null, status: google.maps.places.PlacesServiceStatus) => void
        }) => {
            autocompleteServiceRef.current!.getPlacePredictions({
                input: props.searchText,
                componentRestrictions: props.restrictions,
                // MMC: we return all types of PLACES that google considers, this is not the type of returned parts for a place.
                //types: ["address"],
                sessionToken: sessionTokenRef.current,
            }, props.callback);
        },
        []);

    const getAutocompleteDetails = useCallback(
        (props: {
            placeId: string,
            fields: string[] | undefined,
            callback: (result: google.maps.places.PlaceResult | null, status: google.maps.places.PlacesServiceStatus) => void
        }
        ) => {
            const request: google.maps.places.PlaceDetailsRequest = {
                placeId: props.placeId,
                fields: props.fields,
                sessionToken: sessionTokenRef.current
            }

            placesServiceRef.current!.getDetails(request, props.callback);
        },
        []);

    const geocode = useCallback(
        (
            request: google.maps.GeocoderRequest,
            callback: (results: google.maps.GeocoderResult[] | null | undefined, status: google.maps.GeocoderStatus | null | undefined) => void

        ) => {

            geocoderServiceRef.current!.geocode(request, callback);

        }, []);


    const getPlaceDetails = useCallback(async (props: { placeId: string, fields: string[] | undefined }): Promise<google.maps.places.PlaceResult> => {
        return new Promise((resolve, reject) => {
            const service = placesServiceRef.current;

            if (!service) {
                reject(new Error("Places service not ready"));
                return;
            }

            const request: google.maps.places.PlaceDetailsRequest = {
                placeId: props.placeId,
                fields: props.fields,
                sessionToken: sessionTokenRef.current
            };

            service.getDetails(request, (place, status) => {
                if (status === google.maps.places.PlacesServiceStatus.OK && place) {
                    resolve(place);
                } else {
                    reject(new Error(`Error: ${status}`));
                }
            });
        });
    }, []);

    return {
        mapReady,
        placesReady,
        geocoderReady,
        getAutocompletePredictions,
        getAutocompleteDetails,
        geocode,
        attributionsRef,
        refreshSessionToken,
        geometryReady,
        getPlaceDetails
    };
}