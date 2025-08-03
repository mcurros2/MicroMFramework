import { DefaultProps, Paper, useComponentDefaultProps } from "@mantine/core";
import { Dispatch, PropsWithChildren, ReactNode, RefObject, SetStateAction } from "react";
import { useCreateGoogleMap } from "./useCreateGoogleMap";
import { GoogleMapContext } from "./useGoogleMap";
import { MapOptions } from "./Mapping.types";

export type GoogleMapProps = DefaultProps & PropsWithChildren<{
    mapOptions: MapOptions,
    infoWindowContentRef?: RefObject<HTMLDivElement>,
    setInfoWindowContent?: Dispatch<SetStateAction<ReactNode>>
}>

export const DefaultMapProps: Partial<GoogleMapProps> = {

}

export function GoogleMap(props: GoogleMapProps) {
    props = useComponentDefaultProps('GoogleMap', DefaultMapProps, props);
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { mapOptions, children, infoWindowContentRef, setInfoWindowContent, ...rest } = props;

    const { containerRef, mapContext } = useCreateGoogleMap(props);

    const { renderMarkers } = mapContext;

    return (
        <>
            <Paper style={{ borderRadius: "unset" }} ref={containerRef} {...rest}>
                <GoogleMapContext.Provider value={mapContext}>
                    {renderMarkers()}
                </GoogleMapContext.Provider>
            </Paper>
        </>
    );
}

