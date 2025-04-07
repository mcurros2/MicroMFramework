import { useCallback, useMemo } from "react";

export const MarkerSVGConverterFillColorTAG = '{FILL_COLOR}';
export const MarkerSVGConverterStrokeColorTAG = '{STROKE_COLOR}';
export const MarkerSVGConverterFillOpacityTAG = '{FILL_OPACITY}';
export const MarkerSVGConverterStyleTAG = '{STYLES}';
export const MarkerSVGConverterSelectedStyleTAG = '{SELECTED_STYLE}';

/**
 * Hook that returns a function to convert a SVG image to a URL with the specified colors.
 * it replaces the tags {FILL_COLOR}, {STROKE_COLOR} and {FILL_OPACITY} with the specified colors.
 * @returns
 */
export function useGoogleMarkerSVGConvertToURL() {
    const colorsRegex = useMemo(() => {

        const fillRegex = new RegExp(MarkerSVGConverterFillColorTAG, 'g');
        const strokeRegex = new RegExp(MarkerSVGConverterStrokeColorTAG, 'g');
        const fillOpacityRegex = new RegExp(MarkerSVGConverterFillOpacityTAG, 'g');
        const styleRegex = new RegExp(MarkerSVGConverterStyleTAG, 'g');
        const selectedStyleRegex = new RegExp(MarkerSVGConverterSelectedStyleTAG, 'g');

        return { fillRegex, strokeRegex, fillOpacityRegex, styleRegex, selectedStyleRegex };

    }, []);

    const convertToURL = useCallback((svgImage: string, fillColor?: string, strokeColor?: string, fillOpacity?: number, styles?: string, selectedStyle?: string): {icon: string, url?: string} => {
        if (!svgImage) return ({icon: svgImage});

        // replace image tags
        let icon = svgImage;
        icon = icon.replace(colorsRegex.fillRegex, fillColor ?? '');
        icon = icon.replace(colorsRegex.strokeRegex, strokeColor ?? '');
        icon = icon.replace(colorsRegex.fillOpacityRegex, (fillOpacity ?? 1).toString());
        icon = icon.replace(colorsRegex.styleRegex, styles ?? '');
        icon = icon.replace(colorsRegex.selectedStyleRegex, selectedStyle ?? '');
        
        const url = `data:image/svg+xml;base64,${btoa(icon)}`;

        return {
            icon,
            url
        }

    }, [colorsRegex]);

    return convertToURL;

}