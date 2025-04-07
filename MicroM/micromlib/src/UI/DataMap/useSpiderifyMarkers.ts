//import { useCallback } from "react";

//export const spiderifyMarkers = useCallback((markerObjects) => {
//    const spiderfiedPositions = [];
//    const spiderfiedMarkers = markerObjects.map((marker, index) => {
//        const { lat, lng } = marker.getPosition().toJSON();

//        // Check if any existing spiderfied position is too close
//        const isTooClose = spiderfiedPositions.some(pos => {
//            const distance = google.maps.geometry.spherical.computeDistanceBetween(
//                new google.maps.LatLng(pos.lat, pos.lng),
//                new google.maps.LatLng(lat, lng)
//            );
//            return distance < 20; // Adjust the distance as needed (20 meters here)
//        });

//        if (isTooClose) {
//            // Offset the position
//            const offset = 0.0001 * (index + 1); // Adjust this offset as needed
//            marker.setPosition({ lat: lat + offset, lng: lng + offset });
//        }

//        spiderfiedPositions.push(marker.getPosition().toJSON());
//        return marker;
//    });

//    return spiderfiedMarkers;
//}, []);

