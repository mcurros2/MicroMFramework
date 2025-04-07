import { Skeleton } from "@mantine/core";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { GoogleMap, GoogleMapProps } from "./GoogleMap";
import { GoogleMapRegionSelector } from "./GoogleMapRegionSelector";
import { GoogleMapRegionSelectorProps } from "./GoogleMapRegionSelectorTypes";


export interface GoogleMapRegionSelectorMapProps extends GoogleMapProps {
    regionSelectorProps: Omit<GoogleMapRegionSelectorProps, 'googleMapsAPI'>;
}

export function GoogleMapRegionSelectorMap(props: GoogleMapRegionSelectorMapProps) {
    const { regionSelectorProps, children, ...map_props } = props;

    const API = useGoogleMapsAPI();

    const { mapReady, placesReady, geocoderReady } = API;

    return (
        <>
            {mapReady && placesReady && geocoderReady ?
                (
                    <>
                        <GoogleMap {...map_props}>
                            <GoogleMapRegionSelector {...regionSelectorProps} googleMapsAPI={API} />
                            {children}
                        </GoogleMap>
                        <div style={{display: "none"}} ref={API.attributionsRef}></div>
                    </>
                )
                :
                (<Skeleton height="50vh" />)
            }
        </>
    )

};
