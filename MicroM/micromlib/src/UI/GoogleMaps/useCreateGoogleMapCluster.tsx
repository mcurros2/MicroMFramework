import { MarkerClusterer } from "@googlemaps/markerclusterer";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useDefaultClusterRenderer } from "../DataMap/useDefaultClusterRenderer";
import { GoogleMapClusterProps, MapClusterOptions } from "./GoogleMapCluster";

export function useCreateGoogleMapCluster({
    mapOptions, InitialMarkerClustererOptions, setInfoWindowContent, infoWindowContentRef, labels, markers, selectedMarkers
}: GoogleMapClusterProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    // Autopan in false is beacuse the infowindow when clicked moves the map_ctx and the markerProps gets re-rendered and closes it
    const infoWindowRef = useRef<google.maps.InfoWindow>(new google.maps.InfoWindow({ disableAutoPan: false, headerDisabled: true }));

    const [map, setMap] = useState<google.maps.Map | null>(null);
    const [markerClusterer, setMarkerClusterer] = useState<MarkerClusterer | null>(null);

    const markersRef = useRef<google.maps.Marker[]>([]);
    const currentMarkersRef = useRef<google.maps.Marker[]>([]);

    // Default Cluster Renderer
    const defaultRenderer = useDefaultClusterRenderer(labels?.groupOfLabel, labels?.locationsLabel);

    const currentMarkerClustererOptions = useMemo<MapClusterOptions | undefined>(() => {
        const defaultOptions: MapClusterOptions = {
            algorithmOptions: { maxZoom: 15 },
            renderer: defaultRenderer
        }
        return { ...defaultOptions, ...InitialMarkerClustererOptions };
    }, [defaultRenderer]);

    // Map creation
    useEffect(() => {
        if (map === null && containerRef.current) {
            const initMap = new google.maps.Map(containerRef.current, mapOptions);
            setMap(initMap);

        }
    }, [map, containerRef]);

    // Map options update
    useEffect(() => {
        if (map) map.setOptions(mapOptions);
    }, [map, mapOptions]);

    // MarkerClusterer creation
    useEffect(() => {
        if (map !== null && markerClusterer === null) {
            //console.log("Creating MarkerClusterer")
            const clusterer = new MarkerClusterer({ map, ...currentMarkerClustererOptions });
            setMarkerClusterer(clusterer);

            return () => {
                if (clusterer) {
                    //console.log("Clearing MarkerClusterer")
                    clusterer.clearMarkers(true);
                }
            }

        }
    }, [currentMarkerClustererOptions, map, markerClusterer]);

    // Refresh markers
    const refreshMarkers = useCallback(() => {
        if (markerClusterer !== null && map !== null) {
            // get current markers that are visible in the map_ctx
            const bounds = map.getBounds();
            //console.log("DataMap Bounds", bounds);

            // Filter markers that are visible in the map_ctx
            currentMarkersRef.current = markersRef.current.filter(currentMarker => {
                const pos = currentMarker.getPosition();
                return (bounds && pos && bounds.contains(pos));
            });
            //console.log("DataMap Visible Markers", currentMarkersRef.current.length);

            // Clear and add markers to cluster
            markerClusterer.clearMarkers(true);
            markerClusterer.addMarkers(currentMarkersRef.current);
        }
    }, [map, markerClusterer]);

    // Create markers
    useEffect(() => {
        if (!markers) return;

        markersRef.current = [];

        for (let i = 0; i < markers.length; i++) {
            const markerProps = markers[i];

            let marker: google.maps.Marker;
            const selectedMarker = markerProps.recordIndex !== undefined ? selectedMarkers[markerProps.dataSetId || '']?.[markerProps.recordIndex] : undefined;
            if (markerProps.recordIndex !== undefined && selectedMarker && selectedMarker.dataSetId === markerProps.dataSetId && markerProps.selectedMarkerOptions) {
                marker = new google.maps.Marker({ ...selectedMarker.selectedMarkerOptions });
            } else {
                marker = new google.maps.Marker({ ...markerProps.markerOptions });
            }

            if (infoWindowContentRef?.current && setInfoWindowContent && markerProps.onMarkerClick) {
                marker.addListener("click", () => {
                    const content = markerProps.onMarkerClick!(infoWindowRef.current);
                    if (content) {
                        setInfoWindowContent(content);
                        setTimeout(() => {
                            infoWindowRef.current.setContent(infoWindowContentRef.current);
                            infoWindowRef.current.open({ anchor: marker, map });
                        });
                    }
                });
            }

            markersRef.current.push(marker);
        }

        infoWindowRef.current.close();
        refreshMarkers();

    }, [infoWindowContentRef, map, markers, refreshMarkers, selectedMarkers, setInfoWindowContent]);

    // Map tilesLoaded event, render in cluster manager
    useEffect(() => {
        if (map !== null && markerClusterer !== null) {
            const listener = map.addListener("idle", () => {
                refreshMarkers();
            });
            return () => {
                google.maps.event.removeListener(listener);
            }
        }
    }, [map, markerClusterer, refreshMarkers]);

    const mapContext = useMemo(() => ({
        map,
        renderMarkers: () => { throw new Error("Map cluster doesn't support renderMarkers.") },
        infoWindowRef,
        infoWindowContentRef,
        setInfoWindowContent
    }), [infoWindowContentRef, map, setInfoWindowContent]);

    const returnObject = useMemo(() => ({
        containerRef,
        mapContext
    }), [containerRef, mapContext]);

    return returnObject;
}