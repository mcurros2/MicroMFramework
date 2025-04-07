import { Cluster, ClusterStats, Marker } from "@googlemaps/markerclusterer";
import { useCallback } from "react";


export function useDefaultClusterRenderer(groupOfLabel?: string, locationsLabel?: string) {

    //const { interpolateColor } = useColorTools();

    const defalutRenderer = useCallback((cluster: Cluster, stats: ClusterStats, map: google.maps.Map) => {
        const { count, position } = cluster;

        const color = "#000080";

        // create svg literal with fill color
        const svg = `<svg fill="${color}" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 240" width="50" height="50">
<circle cx="120" cy="120" opacity=".6" r="70" />
<text x="50%" y="50%" style="fill:#fff" text-anchor="middle" font-size="50" dominant-baseline="middle" font-family="roboto,arial,sans-serif">${count}</text>
</svg>`;
        const title = `${groupOfLabel ? (`${groupOfLabel} `) : ''}${count}${locationsLabel ? (` ${locationsLabel}`) : ''}`,
            // adjust zIndex to be above other markers
            zIndex = Number(google.maps.Marker.MAX_ZINDEX) + count;

        const clusterOptions = {
            position,
            zIndex,
            title,
            icon: {
                url: `data:image/svg+xml;base64,${btoa(svg)}`,
                anchor: new google.maps.Point(25, 25),
            },
        };

        return new google.maps.Marker(clusterOptions) as Marker;

    }, [groupOfLabel, locationsLabel]);

    return {
        render: defalutRenderer
    }
}