import { useComponentDefaultProps } from "@mantine/core";
import { useEffect, useMemo, useRef, useState } from "react";
import { useEvent } from "../Core";
import { ClickedRegionData, GoogleMapRegionSelectorDefaultProps, GoogleMapRegionSelectorProps, GoogleMapZoomFeature, SelectedRegionData } from "./GoogleMapRegionSelectorTypes";
import { GoogleMapZoomFeatureStyles } from "./GoogleMapRegionsSelectorStyles";
import { panIfRestricted } from "./functions";
import { GoogleMapsErrorStatus } from "./Mapping.types";
import { useGoogleMap } from "./useGoogleMap";


export function useGoogleMapRegionSelector(props: GoogleMapRegionSelectorProps) {
    const {
        featureTypeMap, onSelectionChanged,
        onRegionClicked, onRegionMouseMove, onAPIError, selectedRegions, setSelectedRegions, googleMapsAPI,
        countries, showOnlyCurrentLayerFeatures, clickedRegions, setClickedRegions, mapRestrictions, mapCenter,
        maxRegions, onMaxRegionsReached, highlightOnMouseOver
    } = useComponentDefaultProps("GoogleMapRegionSelector", GoogleMapRegionSelectorDefaultProps, props);

    const { getPlaceDetails, placesReady } = googleMapsAPI;

    const { map } = useGoogleMap();

    const lastInteractedFeatureIdsRef = useRef<string[]>([]);
    const placeDetailsCache = useRef<Record<string, SelectedRegionData>>({});

    const [currentZoomFeature, setCurrentZoomFeature] = useState<GoogleMapZoomFeature>(featureTypeMap!.low);
    const [activeZoomFeatures, setActiveZoomFeatures] = useState<GoogleMapZoomFeature[]>([featureTypeMap!.low]);

    const stylesByZoomLevel = useMemo(() => ({
        low: featureTypeMap!.low.styles,
        medium: featureTypeMap!.medium.styles,
        high: featureTypeMap!.high.styles,
    }), [featureTypeMap]);

    const emptyStyle = useRef<google.maps.FeatureStyleOptions>({});

    const applyStyle = useEvent((params: google.maps.FeatureStyleFunctionOptions): google.maps.FeatureStyleOptions => {
        const feature = params.feature as google.maps.PlaceFeature;
        const place_id = feature.placeId;
        const featureType = feature.featureType as google.maps.FeatureType;
        const currentLevel = currentZoomFeature.zoomLevel;

        // Determine if the feature should be styled based on its featureType and the zoom level
        let styles: GoogleMapZoomFeatureStyles | undefined;

        if (currentLevel === 'low') {
            if (featureType === featureTypeMap!.low.featureType) {
                styles = stylesByZoomLevel['low'];
            }
        } else if (currentLevel === 'medium') {
            if (featureType === featureTypeMap!.low.featureType) {
                styles = stylesByZoomLevel['low'];
            } else if (featureType === featureTypeMap!.medium.featureType) {
                styles = stylesByZoomLevel['medium'];
            }
        } else if (currentLevel === 'high') {
            if (featureType === featureTypeMap!.low.featureType) {
                styles = stylesByZoomLevel['low'];
            } else if (featureType === featureTypeMap!.medium.featureType) {
                styles = stylesByZoomLevel['medium'];
            } else if (featureType === featureTypeMap!.high.featureType) {
                styles = stylesByZoomLevel['high'];
            }
        }

        // If no styles are found for the feature, return an empty style
        if (!styles) {
            return emptyStyle.current;
        }

        // Apply the appropriate style based on the feature's state
        if (!place_id) {
            return styles.styleDefault!;
        }

        if (clickedRegions[place_id]) {
            return styles.styleClicked!;
        }

        if (lastInteractedFeatureIdsRef.current.includes(place_id)) {
            return styles.styleMouseMove!;
        }

        return styles.styleDefault!;
    });


    const refreshStyles = useEvent(() => {
        if (!map || !featureTypeMap) return;

        if (!showOnlyCurrentLayerFeatures) {
            //console.log('refreshStyles showOnlyCurrentLayerFeatures false');
            const currentLevel = currentZoomFeature.zoomLevel;
            const lowLayer = map.getFeatureLayer(featureTypeMap.low.featureType);
            const mediumLayer = map.getFeatureLayer(featureTypeMap.medium.featureType);
            const highLayer = map.getFeatureLayer(featureTypeMap.high.featureType);

            // Disable levels below the current one
            if (currentLevel === "low") {
                if (mediumLayer !== null) mediumLayer.style = null;
                if (highLayer !== null) highLayer.style = null;

            }
            else if (currentLevel === "medium") {
                if (highLayer !== null) highLayer.style = null;
            }

            if (lowLayer) lowLayer.style = applyStyle;
            if (mediumLayer) mediumLayer.style = applyStyle;
            if (highLayer) highLayer.style = applyStyle;

        } else {
            //console.log('refreshStyles showOnlyCurrentLayerFeatures true');
            // Only show current layer
            const currentLayer = map.getFeatureLayer(currentZoomFeature.featureType);
            if (currentLayer) currentLayer.style = applyStyle;
        }
    });


    const selectFeature = useEvent((features: google.maps.PlaceFeature[]) => {
        if (!map) return;

        if (!placesReady) {
            if (onAPIError) {
                const errorStatus: GoogleMapsErrorStatus = { status: 'PLACES_API_NOT_READY', origin: "Places" };
                onAPIError(errorStatus);
            }
            return;
        }

        // Copy the current selection, we will use it to add the place_id or delete it from the selection
        const internal_clicked_places = { ...clickedRegions };

        // Current number of selected regions
        let currentSelectedCount = Object.keys(internal_clicked_places).length;

        // Track if maxRegions is exceeded
        let maxRegionsExceeded = false;

        for (const feature of features) {
            if (internal_clicked_places[feature.placeId]) {
                delete internal_clicked_places[feature.placeId];
                currentSelectedCount--;
            } else {
                if (currentSelectedCount >= maxRegions!) {
                    maxRegionsExceeded = true;
                    continue;
                }
                const clickedData: ClickedRegionData = { placeId: feature.placeId, featureType: feature.featureType };
                internal_clicked_places[feature.placeId] = clickedData;
                currentSelectedCount++;
            }
        }

        lastInteractedFeatureIdsRef.current = [];

        setClickedRegions(internal_clicked_places);

        if (onSelectionChanged) {
            onSelectionChanged(internal_clicked_places);
        }

        if (maxRegionsExceeded && onMaxRegionsReached) {
            onMaxRegionsReached();
        }

    });


    const handleClick = useEvent((e: google.maps.FeatureMouseEvent) => {
        if (!map) return;
        if (!e.features || e.features.length === 0) return;

        selectFeature(e.features as google.maps.PlaceFeature[]);

        if (onRegionClicked) {
            const placeId = (e.features[0] as google.maps.PlaceFeature).placeId;
            onRegionClicked(placeId);
        }

    });

    const highLightFeature = useEvent((placeIds: string[]) => {
        if (!map) return;

        lastInteractedFeatureIdsRef.current = placeIds;

        const featureType = currentZoomFeature?.featureType;
        const featureLayer = map.getFeatureLayer(featureType!);
        if (featureLayer) {
            featureLayer.style = null;
            featureLayer.style = applyStyle;
        }

    });


    // Mouse move over region
    const handleMouseMove = useEvent((e: google.maps.FeatureMouseEvent) => {
        if (!map) return;
        if (!e.features || e.features.length === 0) return;

        const placeIds = e.features.map((f) => (f as google.maps.PlaceFeature).placeId);

        const allPlaceIdsInLastInteracted = placeIds.every(id => lastInteractedFeatureIdsRef.current.includes(id));
        if (allPlaceIdsInLastInteracted) return;

        //console.log("Mouse move over placeIds:", placeIds)
        highLightFeature(placeIds);

        if (onRegionMouseMove) {
            onRegionMouseMove(placeIds[0]);
        }

    });

    // replace infowindow click event
    useEffect(() => {
        if (!map) return;

        const clickListener = map.addListener("click", (e: google.maps.IconMouseEvent) => {
            if (e.placeId) {
                e.stop();
                //console.log("Map click event", e);
            }
        });

        return () => {
            clickListener.remove();
        };

    }, [map]);

    // Layers
    useEffect(() => {
        if (!map) return;

        const featureType = currentZoomFeature.featureType;

        if (showOnlyCurrentLayerFeatures) {
            // Disable all layers except the current one
            const allFeatureTypes = Object.values(featureTypeMap!);

            allFeatureTypes.forEach(ft => {
                if (ft.featureType === currentZoomFeature?.featureType) return;
                const layer = map.getFeatureLayer(ft.featureType);
                if (layer) {
                    layer.style = null;
                }
            });
        }
        else {
            const currentLevel = currentZoomFeature.zoomLevel;
            const mediumLayer = map.getFeatureLayer(featureTypeMap!.medium.featureType);
            const highLayer = map.getFeatureLayer(featureTypeMap!.high.featureType);

            // Disable levels below the current one
            if (currentLevel === "low") {
                mediumLayer.style = null;
                highLayer.style = null;
            }
            else if (currentLevel === "medium") {
                highLayer.style = null;
            }

        }

        const featureLayer = map.getFeatureLayer(featureType!);
        if (featureLayer) {
            //console.log('useEffect layers apply style');
            featureLayer.style = applyStyle;

            const clickListener = featureLayer.addListener("click", handleClick);
            const moveListener = highlightOnMouseOver ? featureLayer.addListener("mousemove", handleMouseMove) : undefined;

            return () => {
                clickListener.remove();
                if (moveListener) moveListener.remove();
            };
        } else {
            console.warn(`Feature layer '${featureType}' not found.`);
        }

    }, [applyStyle, currentZoomFeature.featureType, currentZoomFeature.zoomLevel, featureTypeMap, handleClick, handleMouseMove, highlightOnMouseOver, map, showOnlyCurrentLayerFeatures]);

    // fit to bounds
    useEffect(() => {
        if (!map || !mapRestrictions) return;

        const dragListener = map.addListener('dragend', () => {
            panIfRestricted(map, mapRestrictions, mapCenter!)
        });

        const zoomListener = map.addListener('zoom_changed', () => {
            panIfRestricted(map, mapRestrictions, mapCenter!)
        });

        return (
            () => {
                dragListener.remove();
                zoomListener.remove();
            }
        )

    }, [map, mapCenter, mapRestrictions]);

    // Zoom
    useEffect(() => {
        if (!map) return;

        const handleZoomChange = () => {
            const zoom = map.getZoom();

            if (zoom === undefined || featureTypeMap === undefined) return;
            let newFeatureType: GoogleMapZoomFeature;

            if (zoom <= featureTypeMap.low.zoom) {
                newFeatureType = featureTypeMap.low;
                setActiveZoomFeatures([featureTypeMap.low]);
            } else if (zoom > featureTypeMap.low.zoom && zoom <= featureTypeMap.medium.zoom) {
                newFeatureType = featureTypeMap.medium;
                if (showOnlyCurrentLayerFeatures) {
                    setActiveZoomFeatures([featureTypeMap.medium]);
                }
                else {
                    setActiveZoomFeatures([featureTypeMap.low, featureTypeMap.medium]);
                }
            } else {
                newFeatureType = featureTypeMap.high;
                if (showOnlyCurrentLayerFeatures) {
                    setActiveZoomFeatures([featureTypeMap.high]);
                }
                else {
                    setActiveZoomFeatures([featureTypeMap.low, featureTypeMap.medium, featureTypeMap.high]);
                }
            }

            setCurrentZoomFeature(newFeatureType);
        };

        const zoomListener = map.addListener("zoom_changed", handleZoomChange);

        // Initialize zoom level
        handleZoomChange();

        return () => {
            zoomListener.remove();
            //google.maps.event.removeListener(zoomListener);
        };
    }, [featureTypeMap, map, setActiveZoomFeatures, setCurrentZoomFeature, showOnlyCurrentLayerFeatures]);



    // Get place details
    const updateSequence = useRef(0);
    useEffect(() => {
        const currentPlaceEntries = Object.entries(clickedRegions);
        const existingPlaceData = Object.entries(selectedRegions);

        const existingPlaceMap = new Map(existingPlaceData);

        const newPlaceEntries = currentPlaceEntries.filter(([id, regionData]) => !existingPlaceMap.has(id));

        const currentPlaceMap = new Map(currentPlaceEntries);

        const removedPlaceEntries = existingPlaceData.filter(([id, selectedData]) => !currentPlaceMap.has(id));

        if (removedPlaceEntries.length > 0) {
            //console.log("Removing place details for placeIds:", removedPlaceEntries.map(([id]) => id));
            setSelectedRegions(prevDetails => {
                const updatedDetails = { ...prevDetails };
                removedPlaceEntries.forEach(([id]) => {
                    delete updatedDetails[id];
                });
                return updatedDetails;
            });
        }

        const sequence = ++updateSequence.current;

        newPlaceEntries.forEach(([placeId, clickedData]) => {
            const cachedDetails = placeDetailsCache.current[placeId];

            if (cachedDetails) {
                //console.log("Using cached details for place_id:", place_id);
                setSelectedRegions((prev) => {
                    const updated_selection = { ...prev };
                    updated_selection[placeId] = cachedDetails;
                    return updated_selection;
                });
            }
            else {
                //console.log("Fetching place details for place_id:", place_id)
                getPlaceDetails(
                    {
                        placeId: placeId,
                        fields: ['address_components'],
                    }
                ).then(place => {
                    if (sequence === updateSequence.current) {
                        const addressComponents = place.address_components || [];
                        const countryComponent = addressComponents.find(comp => comp.types.includes('country'));
                        const adminLevel1Component = addressComponents.find(comp => comp.types.includes('administrative_area_level_1'));
                        const adminLevel2Component = addressComponents.find(comp => comp.types.includes('administrative_area_level_2'));
                        const postalCodeComponent = addressComponents.find(comp => comp.types.includes('postal_code'));
                        const localityComponent = addressComponents.find(comp => comp.types.includes('locality'));

                        if (countries && countryComponent && !countries.includes(countryComponent.short_name!)) {
                            //console.log("Country not in list of allowed countries:", countryComponent.short_name);
                            setSelectedRegions((prev) => {
                                const updated_selection = { ...prev };
                                delete updated_selection[placeId];
                                return updated_selection;
                            });
                            setClickedRegions((prev) => {
                                const updated_selection = { ...prev };
                                delete updated_selection[placeId];
                                return updated_selection;
                            });
                        }
                        else {
                            //console.log("Place details fetched for place_id:", place_id);
                            placeDetailsCache.current[placeId] = {
                                placeId: placeId,
                                featureType: clickedData.featureType,

                                countryId: countryComponent?.short_name,
                                countryName: countryComponent?.long_name,

                                administrativeAreaLevel1Name: adminLevel1Component?.long_name,
                                administrativeAreaLevel1Id: adminLevel1Component?.short_name,

                                administrativeAreaLevel2Name: adminLevel2Component?.long_name,
                                administrativeAreaLevel2Id: adminLevel2Component?.short_name,

                                localityId: localityComponent?.short_name,
                                localityName: localityComponent?.long_name,

                                postalCode: postalCodeComponent?.short_name,
                            } as SelectedRegionData;

                            setSelectedRegions((prev) => {
                                // Update the selected region with addressComponents.
                                const updated_selection = { ...prev };
                                const new_region_data: SelectedRegionData = {
                                    placeId: placeId,
                                    featureType: clickedData.featureType,
                                    countryId: countryComponent?.short_name,
                                    countryName: countryComponent?.long_name,
                                    administrativeAreaLevel1Name: adminLevel1Component?.long_name,
                                    administrativeAreaLevel1Id: adminLevel1Component?.short_name,
                                    administrativeAreaLevel2Name: adminLevel2Component?.long_name,
                                    administrativeAreaLevel2Id: adminLevel2Component?.short_name,
                                    localityId: localityComponent?.short_name,
                                    localityName: localityComponent?.long_name,
                                    postalCode: postalCodeComponent?.short_name,
                                } as SelectedRegionData;

                                updated_selection[placeId] = new_region_data;

                                return updated_selection;
                            });
                        }
                    }
                }).catch(error => {
                    console.error("Error fetching place details:", error);

                    if (onAPIError) {
                        const errorStatus: GoogleMapsErrorStatus = { status: 'APIERROR', origin: "Places" };
                        if (onAPIError) onAPIError(errorStatus);
                    }
                });
            }
        });


    }, [clickedRegions, countries, getPlaceDetails, onAPIError, selectedRegions, setClickedRegions, setSelectedRegions]);

    // apply styles on state change
    useEffect(() => {
        if (!map) return;

        const featureType = currentZoomFeature?.featureType;
        const featureLayer = map.getFeatureLayer(featureType!);
        if (featureLayer) {
            featureLayer.style = null;
            featureLayer.style = applyStyle;
        }

    }, [clickedRegions]);

    return {
        applyStyle,
        selectFeature,
        highLightFeature,
        currentZoomFeature,
        setCurrentZoomFeature,
        map,
        handleClick,
        handleMouseMove,
        activeZoomFeatures,
        setActiveZoomFeatures,
        refreshStyles
    }

}

