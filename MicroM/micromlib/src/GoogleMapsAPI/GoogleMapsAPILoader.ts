import { Loader, LoaderOptions } from '@googlemaps/js-api-loader';
import { useEffect, useState } from 'react';

export const GoogleMapsAPILoaderConfig:Partial<LoaderOptions> = {
    apiKey: "",
}

let googleAPILoaderInstance:Loader;
export function GetGoogleMapsAPILoader() {
    return googleAPILoaderInstance ?? (googleAPILoaderInstance = new Loader(GoogleMapsAPILoaderConfig as LoaderOptions));
}

export function useGooglePlacesAPIloader() {
    const [isReady, setIsReady] = useState(false);

    useEffect(() => {
        if (!window.google?.maps.places) {
            GetGoogleMapsAPILoader().importLibrary("places")
              .then(_callback);
        } else {
            _callback();
        }
    }, []);

    function _callback() {
        setIsReady(true);
    }

    return [isReady];
}

export function useGoogleGeocoderAPIloader() {
    const [isReady, setIsReady] = useState(false);

    useEffect(() => {
        if (!window.google?.maps.Geocoder) {
            GetGoogleMapsAPILoader().importLibrary("geocoding")
                .then(_callback);
        } else {
            _callback();
        }
    }, []);

    function _callback() {
        setIsReady(true);
    }

    return [isReady];
}

export function useGoogleMapsAPILoader() {
    const [isReady, setIsReady] = useState(false);

    useEffect(() => {
        if (!window.google?.maps.Map) {
            GetGoogleMapsAPILoader().importLibrary("maps")
            .then(_callback);
        } else {
            _callback();
        }
    }, []);

    function _callback() {
        setIsReady(true);
    }

    return [isReady];
}

export function useGoogleGeometryAPILoader() {
    const [isReady, setIsReady] = useState(false);

    useEffect(() => {
        if (!window.google?.maps.Map) {
            GetGoogleMapsAPILoader().importLibrary("geometry")
                .then(_callback);
        } else {
            _callback();
        }
    }, []);

    function _callback() {
        setIsReady(true);
    }

    return [isReady];
}
