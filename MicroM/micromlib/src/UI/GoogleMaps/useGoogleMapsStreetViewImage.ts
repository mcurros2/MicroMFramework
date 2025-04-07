import { GoogleMapsAPILoaderConfig } from "../../GoogleMapsAPI";

export function getGoogleMapsStreetViewImage(location: google.maps.LatLng | google.maps.LatLngLiteral, width: number = 350, height: number = 200, fov: number = 90) {

    const streetViewUrl = `https://maps.googleapis.com/maps/api/streetview?size=${width}x${height}&location=${location.lat},${location.lng}&fov=${fov}&key=${GoogleMapsAPILoaderConfig.apiKey}`;
    return streetViewUrl;

}