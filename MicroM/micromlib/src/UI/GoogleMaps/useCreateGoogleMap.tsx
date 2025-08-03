import { Children, isValidElement, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { GoogleMapProps } from "./GoogleMap";
import { GoogleMapRegionSelector } from "./GoogleMapRegionSelector";
import { GoogleMarker } from "./GoogleMarker";
import { DEFAULT_MAP_CENTER } from "./Mapping.types";

export function useCreateGoogleMap({ mapOptions, children, setInfoWindowContent, infoWindowContentRef }: GoogleMapProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const [map, setMap] = useState<google.maps.Map | null>(null);

    const infoWindowRef = useRef<google.maps.InfoWindow>(new google.maps.InfoWindow());

    useEffect(() => {
        if (map === null) {
            if (!containerRef.current) throw new Error("Container required.");

            if (mapOptions && mapOptions.center && (!mapOptions.center.lat || !mapOptions.center.lng)) { mapOptions.center = undefined; }

            const initMap = new google.maps.Map(containerRef.current, mapOptions);
            setMap(initMap);
        }
    }, [map, mapOptions]);

    useEffect(() => {
        if (mapOptions && mapOptions.center && (!mapOptions.center.lat || !mapOptions.center.lng)) { mapOptions.center = undefined; }

        if (map) map.setOptions(mapOptions);
    }, [map, mapOptions]);

    useEffect(() => {
        return () => {
            if (map !== null) {
                google.maps.event.clearInstanceListeners(map);
                map.unbindAll();
            }
        }
    }, [map]);

    const renderMarkers = useCallback(() => {
        if (map === null) {
            return null;
        }
        Children.forEach(children, child => {
            if (!isValidElement(child) || (child.type !== GoogleMarker && child.type !== GoogleMapRegionSelector)) {
                throw new Error("Invalid children component.");
            }
        });
        return children;

    }, [children, map]);

    const mapContext = useMemo(() => ({
        map,
        renderMarkers,
        infoWindowRef,
        infoWindowContentRef,
        setInfoWindowContent
    }), [infoWindowContentRef, map, renderMarkers, setInfoWindowContent]);

    const returnObject = useMemo(() => ({
        containerRef,
        mapContext
    }), [containerRef, mapContext]);

    return returnObject;
}