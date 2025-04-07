import { Dispatch, SetStateAction } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI";
import { ValuesObject } from "../../client";
import { GoogleMapHighZoomStyleDefaultProps, GoogleMapLowZoomStyleDefaultProps, GoogleMapMediumZoomStyleDefaultProps, GoogleMapZoomFeatureStyles } from "./GoogleMapRegionsSelectorStyles";
import { GoogleMapsErrorStatus } from "./Mapping.types";


export type GoogleMapZoomLevel = 'low' | 'medium' | 'high';

export interface GoogleMapZoomFeature {
    featureType: google.maps.FeatureType,
    zoom: number,
    styles: GoogleMapZoomFeatureStyles,
    zoomLevel: GoogleMapZoomLevel,
}

export interface GoogleMapZoomFeatureTypeMap {
    low: GoogleMapZoomFeature,
    medium: GoogleMapZoomFeature,
    high: GoogleMapZoomFeature,
}

export interface ISelectedRegionData {
    placeId: string;

    featureType?: google.maps.FeatureType;

    countryId?: string;
    countryName?: string,

    administrativeAreaLevel1Id?: string;
    administrativeAreaLevel1Name?: string;

    administrativeAreaLevel2Id?: string;
    administrativeAreaLevel2Name?: string;

    localityId?: string;
    localityName?: string;

    postalCode?: string;
}

export type SelectedRegionData = ISelectedRegionData & ValuesObject

export interface IClickedRegionData {
    placeId: string;
    featureType?: google.maps.FeatureType;
    appliedStyle?: google.maps.FeatureStyleOptions
}

export type ClickedRegionData = IClickedRegionData & ValuesObject

export interface GoogleMapRegionSelectorProps {
    googleMapsAPI: ReturnType<typeof useGoogleMapsAPI>,
    selectedRegions: Record<string, SelectedRegionData>,
    setSelectedRegions: Dispatch<SetStateAction<Record<string, SelectedRegionData>>>,
    clickedRegions: Record<string, ClickedRegionData>,
    setClickedRegions: Dispatch<SetStateAction<Record<string, ClickedRegionData>>>,
    featureTypeMap?: GoogleMapZoomFeatureTypeMap,
    onSelectionChanged?: (selectedRegions: Record<string, SelectedRegionData>) => void,
    onRegionClicked?: (placeId: string) => void,
    onRegionMouseMove?: (placeId: string) => void,
    onAPIError?: (status: GoogleMapsErrorStatus) => void,
    countries: string[],
    showOnlyCurrentLayerFeatures?: boolean,
    mapRestrictions?: google.maps.LatLngBoundsLiteral[],
    mapCenter?: google.maps.LatLngLiteral,
    maxRegions?: number,
    onMaxRegionsReached?: () => void,
    highlightOnMouseOver?: boolean,
}

export const GoogleMapRegionSelectorDefaultProps: Partial<GoogleMapRegionSelectorProps> = {
    featureTypeMap: {
        // We use string constants here because we are using the Google Maps API and it will not be available until loaded
        low: { featureType: "ADMINISTRATIVE_AREA_LEVEL_1" as google.maps.FeatureType, zoom: 6, styles: GoogleMapLowZoomStyleDefaultProps, zoomLevel: 'low' },
        medium: {
            featureType: "ADMINISTRATIVE_AREA_LEVEL_2" as google.maps.FeatureType, zoom: 8, styles: GoogleMapMediumZoomStyleDefaultProps, zoomLevel: 'medium'
        },
        high: { featureType: "POSTAL_CODE" as google.maps.FeatureType, zoom: 11, styles: GoogleMapHighZoomStyleDefaultProps, zoomLevel: 'high' },
    },
};

