import { ReactNode, useEffect, useRef } from "react";
import { useGoogleMap } from "./useGoogleMap";
import { useComponentDefaultProps } from "@mantine/core";


export type MarkerOptions = Omit<google.maps.MarkerOptions, "map">

export type OnDragEndCallback = (position: google.maps.LatLng) => void

export interface MarkerProps {
    markerOptions: MarkerOptions,
    onDragEnd?: OnDragEndCallback,
    onMarkerClick?: (infoWindow: google.maps.InfoWindow) => ReactNode,
    group?: string,
    dataSetId?: string,
    recordIndex?: number,
    selectedOrder?: number,
    selectedMarkerOptions?: MarkerOptions,
    isMapCenter?: boolean,
}

export const MarkerPropsDefault: Partial<MarkerProps> = {
    dataSetId: "default",
}


export function GoogleMarker(props: MarkerProps) {
    const { markerOptions, onDragEnd, onMarkerClick } = useComponentDefaultProps('GoogleMarker', MarkerPropsDefault, props);

    const { map, infoWindowContentRef, infoWindowRef, setInfoWindowContent } = useGoogleMap();
    const markerRef = useRef<google.maps.Marker | null>(null);

    useEffect(() => {
        if (map && !markerRef.current) {
            markerRef.current = new google.maps.Marker({ ...markerOptions, map });
        }
    }, [map, markerOptions]);

    useEffect(() => {
        if (markerRef.current) {
            markerRef.current.setOptions(markerOptions);
        }
    }, [markerOptions]);

    useEffect(() => {
        if (!map) return;
        const listener = onDragEnd && markerRef.current?.addListener("dragend", () => {
            const pos = markerRef.current!.getPosition();
            if (pos) {
                onDragEnd!(pos);
            }
        });
        return () => {
            if (listener) {
                google.maps.event.removeListener(listener);
            }
        };
    }, [map, onDragEnd]);

    // InfoWindow
    useEffect(() => {
        if (!map || !infoWindowContentRef?.current || !setInfoWindowContent) return;
        const listener = onMarkerClick && markerRef.current?.addListener("click", () => {
            const content = onMarkerClick!(infoWindowRef.current!);
            if (content) {
                setInfoWindowContent(content);
                infoWindowRef.current!.setContent(infoWindowContentRef.current);
                infoWindowRef.current!.open(map, markerRef.current);
            }
        });
        return () => {
            if (listener) {
                google.maps.event.removeListener(listener);
            }
        };
    }, [infoWindowContentRef, infoWindowRef, map, onMarkerClick, setInfoWindowContent]);

    useEffect(() => {
        return () => {
            if (markerRef.current) {
                google.maps.event.clearInstanceListeners(markerRef.current);
                markerRef.current.setMap(null);
                markerRef.current.unbindAll();
                markerRef.current = null;
            }
        }
    }, []);

    return undefined;
}
