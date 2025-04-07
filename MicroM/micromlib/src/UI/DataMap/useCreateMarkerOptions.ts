import { useCallback } from "react";
import { stringToColor, stringToColorLabel } from "./colorTools";
import { MarkerSVGConverterFillColorTAG, MarkerSVGConverterFillOpacityTAG, MarkerSVGConverterSelectedStyleTAG, MarkerSVGConverterStyleTAG, useGoogleMarkerSVGConvertToURL } from "./useGoogleMarkerSVGConverter";


export const MapMarkerDefaultShadowStyle = "-webkit-filter: drop-shadow( 3px 3px 2px rgba(0, 0, 0, .7)); filter: drop-shadow( 3px 3px 2px rgba(0, 0, 0, .7));";
//export const MapMarkerDefaultSelectedStyle = "-webkit-filter: drop-shadow(3px 3px 0px rgba(220, 20, 60, 1)); filter: drop-shadow(3px 3px 0px rgba(220, 20, 60, 1));";
export const MapMarkerDefaultSelectedStyle = "-webkit-filter: drop-shadow(0px 0px 3px rgba(0, 0, 0, .8)); filter: drop-shadow(0px 0px 3px rgba(0, 0, 0, .8));";
//export const MapMarkerDefaultSelectedStyle = "";

export const MapMarkerCircleFilledSVGIcon = `<svg  xmlns="http://www.w3.org/2000/svg" style="${MarkerSVGConverterStyleTAG}${MarkerSVGConverterSelectedStyleTAG}" width="24"  height="24"  viewBox="0 0 24 24"  fill="${MarkerSVGConverterFillColorTAG}"  class="icon icon-tabler icons-tabler-filled icon-tabler-circle"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M7 3.34a10 10 0 1 1 -4.995 8.984l-.005 -.324l.005 -.324a10 10 0 0 1 4.995 -8.336z" /></svg>`;

export const MapMarkerCircleCheckedSVGIcon = `<svg  xmlns="http://www.w3.org/2000/svg" style="${MarkerSVGConverterStyleTAG}${MarkerSVGConverterSelectedStyleTAG}" width="24"  height="24"  viewBox="0 0 24 24"  fill="${MarkerSVGConverterFillColorTAG}"  class="icon icon-tabler icons-tabler-filled icon-tabler-circle-check"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M17 3.34a10 10 0 1 1 -14.995 8.984l-.005 -.324l.005 -.324a10 10 0 0 1 14.995 -8.336zm-1.293 5.953a1 1 0 0 0 -1.32 -.083l-.094 .083l-3.293 3.292l-1.293 -1.292l-.094 -.083a1 1 0 0 0 -1.403 1.403l.083 .094l2 2l.094 .083a1 1 0 0 0 1.226 0l.094 -.083l4 -4l.083 -.094a1 1 0 0 0 -.083 -1.32z" /></svg>`;

export const MapMarkerDefaultSelectedSVGIcon = MapMarkerCircleFilledSVGIcon;


export const MapMarkerDefaultSVGIcon = `<svg xmlns="http://www.w3.org/2000/svg" style="${MarkerSVGConverterStyleTAG}${MarkerSVGConverterSelectedStyleTAG}" width="24" height="24" viewBox="0 0 24 24" fill="${MarkerSVGConverterFillColorTAG}" fill-opacity="${MarkerSVGConverterFillOpacityTAG}" class="icon icon-tabler icons-tabler-filled icon-tabler-map-pin"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M18.364 4.636a9 9 0 0 1 .203 12.519l-.203 .21l-4.243 4.242a3 3 0 0 1 -4.097 .135l-.144 -.135l-4.244 -4.243a9 9 0 0 1 12.728 -12.728zm-6.364 3.364a3 3 0 1 0 0 6a3 3 0 0 0 0 -6z" /></svg>`;
export const MapMarkerDefaultFillColor = '#1e72c1';
export const MapMarkerDefaultFillOpacity = .7;

export const MapMarkerGroupDefaultSVGIcon = `<svg  xmlns="http://www.w3.org/2000/svg" style="${MarkerSVGConverterStyleTAG}${MarkerSVGConverterSelectedStyleTAG}" width="24"  height="24"  viewBox="0 0 24 24" fill="${MarkerSVGConverterFillColorTAG}" fill-opacity="${MarkerSVGConverterFillOpacityTAG}" class="icon icon-tabler icons-tabler-filled icon-tabler-hexagon"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M10.425 1.414l-6.775 3.996a3.21 3.21 0 0 0 -1.65 2.807v7.285a3.226 3.226 0 0 0 1.678 2.826l6.695 4.237c1.034 .57 2.22 .57 3.2 .032l6.804 -4.302c.98 -.537 1.623 -1.618 1.623 -2.793v-7.284l-.005 -.204a3.223 3.223 0 0 0 -1.284 -2.39l-.107 -.075l-.007 -.007a1.074 1.074 0 0 0 -.181 -.133l-6.776 -3.995a3.33 3.33 0 0 0 -3.216 0z" /></svg>`;
export const MapMarkerGroupDefaultFillColor = 'red';
export const MapMarkerGroupDefaultLabelOrigin = { x: 20, y: 19 };

export interface UseCreateMarkerOptionsProps {
    svgIcon?: string,
    fillColor?: string,
    strokeColor?: string,
    fillOpacity?: number,
    styles?: string,

    selectedSvgIcon?: string,
    selectedFillColor?: string,
    selectedStrokeColor?: string,
    selectedFillOpacity?: number,
    selectedStyles?: string,
    selectedLabelOrigin?: google.maps.Point,
    selectedLabel?: google.maps.MarkerLabel,

    position: google.maps.LatLng | google.maps.LatLngLiteral,
    assignColorByValue?: string,

    title?: string,

    dataSetId?: string,
    recordIndex?: number,

    //markerUniqueKey?: string,
    labelOrigin?: google.maps.Point,
    label?: google.maps.MarkerLabel,

    isMapCenter?: boolean;
}

export function useCreateMarkerOptions() {
    const svgConverter = useGoogleMarkerSVGConvertToURL();



    const createMarkerOptions = useCallback((props: UseCreateMarkerOptionsProps) => {
        const {
            svgIcon, fillColor, strokeColor, fillOpacity, position, assignColorByValue, title, label, labelOrigin, styles,
            selectedSvgIcon, selectedFillColor, selectedStrokeColor, selectedFillOpacity, selectedStyles, selectedLabel, selectedLabelOrigin, isMapCenter
        } = props;

        const markerOptions: google.maps.MarkerOptions = { position, title };

        let newFillColor = fillColor ?? MapMarkerDefaultFillColor;
        if (assignColorByValue) {
            newFillColor = stringToColor(assignColorByValue);
        }

        const icon = svgConverter(svgIcon ? svgIcon : MapMarkerDefaultSVGIcon, newFillColor, strokeColor, fillOpacity ? fillOpacity : MapMarkerDefaultFillOpacity, styles ?? MapMarkerDefaultShadowStyle, '');

        // This is to render correctly svg icons with styles
        markerOptions.optimized = false;

        markerOptions.icon = {
            url: icon.url!,
            scaledSize: new google.maps.Size(40, 40),
            origin: new google.maps.Point(0, 0),
            anchor: new google.maps.Point(20, 40),
        };

        if (label) {
            if (assignColorByValue) label.color = stringToColorLabel(assignColorByValue);
            markerOptions.label = label;
        }

        if (labelOrigin) {
            markerOptions.icon.labelOrigin = labelOrigin;
        }

        const selectedMarkerOptions: google.maps.MarkerOptions = { ...markerOptions };
        delete selectedMarkerOptions.label;

        const selectedIcon = svgConverter(selectedSvgIcon ?? MapMarkerDefaultSelectedSVGIcon ?? svgIcon ?? MapMarkerDefaultSVGIcon, selectedFillColor ?? newFillColor, selectedStrokeColor ?? strokeColor, selectedFillOpacity ?? fillOpacity ?? MapMarkerDefaultFillOpacity, '', selectedStyles ?? MapMarkerDefaultSelectedStyle);
        selectedMarkerOptions.icon = {
            url: selectedIcon.url!,
            scaledSize: new google.maps.Size(40, 40),
            origin: new google.maps.Point(0, 0),
            anchor: new google.maps.Point(20, 40),
        };

        if (selectedLabel) {
            selectedMarkerOptions.label = label;
        }

        if (selectedLabelOrigin) {
            selectedMarkerOptions.icon.labelOrigin = labelOrigin;
        }

        return { markerOptions, selectedMarkerOptions, isMapCenter };

    }, [svgConverter]);


    return createMarkerOptions;

}