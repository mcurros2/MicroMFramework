import { MarkerClustererOptions } from "@googlemaps/markerclusterer";
import { DefaultProps, Paper, useComponentDefaultProps } from "@mantine/core";
import { Dispatch, PropsWithChildren, ReactNode, RefObject, SetStateAction } from "react";
import { useCreateGoogleMapCluster } from "./useCreateGoogleMapCluster";
import { GoogleMapContext } from "./useGoogleMap";
import { MapOptions } from "./Mapping.types";
import { MarkerProps } from "./GoogleMarker";


export interface MapClusterOptions extends Omit<MarkerClustererOptions, 'map' | 'markers'> { }

export type SelectedMarkerProps = Record<string, Record<number, MarkerProps>>

export type GoogleMapClusterProps = DefaultProps & PropsWithChildren<{
    mapOptions: MapOptions,
    InitialMarkerClustererOptions?: MapClusterOptions,
    infoWindowContentRef?: RefObject<HTMLDivElement>,
    setInfoWindowContent?: Dispatch<SetStateAction<ReactNode>>,
    markers?: MarkerProps[],
    labels?: {
        groupOfLabel?: string,
        locationsLabel?: string,
    },
    selectedMarkers: SelectedMarkerProps,
}>

export const DefaultMapClusterProps: Partial<GoogleMapClusterProps> = {
    labels: {
        groupOfLabel: 'Group of',
        locationsLabel: 'locations',
    },
}

export function GoogleMapCluster(props: GoogleMapClusterProps) {
    props = useComponentDefaultProps('GoogleMapCluster', DefaultMapClusterProps, props);
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { mapOptions, InitialMarkerClustererOptions, children, infoWindowContentRef, setInfoWindowContent, markers, labels, selectedMarkers, ...rest } = props;

    const { containerRef, mapContext } = useCreateGoogleMapCluster(props);

    return (
        <>
            <Paper style={{ borderRadius: "unset" }} ref={containerRef} {...rest}>
                <GoogleMapContext.Provider value={mapContext}>
                </GoogleMapContext.Provider>
            </Paper>
        </>
    );
}

