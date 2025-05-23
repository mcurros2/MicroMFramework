/**
 * Converts a hexadecimal color string to an RGB array.
 * @param hex - The hexadecimal color string.
 * @returns The RGB array.
 */
export function hexToRgb(hex: string): [number, number, number] {
    const bigint = parseInt(hex.slice(1), 16);
    const r = (bigint >> 16) & 255;
    const g = (bigint >> 8) & 255;
    const b = bigint & 255;
    return [r, g, b];
}

/**
 * Converts an RGB array to a hexadecimal color string.
 * @param r - The red component.
 * @param g - The green component.
 * @param b - The blue component.
 * @returns The hexadecimal color string.
 */
export function rgbToHex(r: number, g: number, b: number): string {
    return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
}

/**
 * Interpolates between two colors.
 * @param color1 - The first color.
 * @param color2 - The second color.
 * @param factor - The interpolation factor.
 * @returns The interpolated color.
 */
export function interpolateColor(color1: string, color2: string, factor: number): string {
    const [r1, g1, b1] = hexToRgb(color1);
    const [r2, g2, b2] = hexToRgb(color2);

    const r = Math.round(r1 + factor * (r2 - r1));
    const g = Math.round(g1 + factor * (g2 - g1));
    const b = Math.round(b1 + factor * (b2 - b1));

    return rgbToHex(r, g, b);
}

/**
 * Computes a hash and converts a string to a color. Use this function to generate a color from a string from a small set of strings.
 * @param str - The string to convert.
 * @returns The color.
 */
export function stringToColor(str: string): string {
    // (DJB2)
    let hash = 5381;
    for (let i = 0; i < str.length; i++) {
        hash = (hash * 33) ^ str.charCodeAt(i);
    }

    let color = '#';
    for (let i = 0; i < 3; i++) {
        const value = (hash >> (i * 8)) & 0xFF;
        color += ('00' + value.toString(16)).slice(-2);
    }

    return color;
}

/**
 * Computes a label color (either white or black) based on the background color generated by stringToColor.
 * @param str - The string to convert to a color and use as background.
 * @returns The label color.
 */
export function stringToColorLabel(str: string): string {
    // Generate background color from string
    const bgColor = stringToColor(str);

    // Convert hex background color to RGB
    const [r, g, b] = hexToRgb(bgColor);

    // Calculate brightness using the YIQ formula
    const brightness = (r * 299 + g * 587 + b * 114) / 1000;

    // Return white or black label color based on brightness
    return brightness > 128 ? '#000000' : '#FFFFFF';
}