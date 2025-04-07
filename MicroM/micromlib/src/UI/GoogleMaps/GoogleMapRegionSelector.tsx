import { useComponentDefaultProps } from "@mantine/core";
import { GoogleMapRegionSelectorDefaultProps, GoogleMapRegionSelectorProps } from "./GoogleMapRegionSelectorTypes";
import { useGoogleMapRegionSelector } from "./useGoogleMapRegionSelector";



export function GoogleMapRegionSelector(props: GoogleMapRegionSelectorProps) {
    const mergedProps = useComponentDefaultProps("GoogleMapRegionSelector", GoogleMapRegionSelectorDefaultProps, props);

    useGoogleMapRegionSelector(mergedProps);

    return null; // Not a visual React component
}
