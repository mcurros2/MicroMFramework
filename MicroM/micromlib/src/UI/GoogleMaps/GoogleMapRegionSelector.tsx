import { useProps } from "@mantine/core";
import { GoogleMapRegionSelectorDefaultProps, GoogleMapRegionSelectorProps } from "./GoogleMapRegionSelectorTypes";
import { useGoogleMapRegionSelector } from "./useGoogleMapRegionSelector";



export function GoogleMapRegionSelector(props: GoogleMapRegionSelectorProps) {
    const mergedProps = useProps("GoogleMapRegionSelector", GoogleMapRegionSelectorDefaultProps, props);

    useGoogleMapRegionSelector(mergedProps);

    return null; // Not a visual React component
}

