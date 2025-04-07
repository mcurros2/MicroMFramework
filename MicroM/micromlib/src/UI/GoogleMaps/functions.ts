
export const isWithinRestrictions = (latLng: google.maps.LatLngLiteral, boundsRestrictions: google.maps.LatLngBoundsLiteral[]) => {
    return boundsRestrictions.some(boundsLiteral => {
        const bounds = new google.maps.LatLngBounds(
            new google.maps.LatLng(boundsLiteral.south, boundsLiteral.west),
            new google.maps.LatLng(boundsLiteral.north, boundsLiteral.east)
        );
        return bounds.contains(latLng);
    });
};

export const findClosestCenter = (latLng: google.maps.LatLngLiteral, boundsRestrictions: google.maps.LatLngBoundsLiteral[], defaultCenter: google.maps.LatLngLiteral) => {
    let closestDistance = Infinity;
    let closestPoint = defaultCenter;

    boundsRestrictions.forEach(boundsLiteral => {
        const bounds = new google.maps.LatLngBounds(
            new google.maps.LatLng(boundsLiteral.south, boundsLiteral.west),
            new google.maps.LatLng(boundsLiteral.north, boundsLiteral.east)
        );

        if (bounds.contains(latLng)) {
            closestPoint = latLng;
            return;
        }

        const ne = bounds.getNorthEast();
        const sw = bounds.getSouthWest();

        // Clamping latitude y longitude a los límites
        const lat = Math.max(sw.lat(), Math.min(latLng.lat, ne.lat()));
        const lng = Math.max(sw.lng(), Math.min(latLng.lng, ne.lng()));

        const clampedLatLng = new google.maps.LatLng(lat, lng);
        const distance = google.maps.geometry.spherical.computeDistanceBetween(latLng, clampedLatLng);

        if (distance < closestDistance) {
            closestDistance = distance;
            closestPoint = clampedLatLng.toJSON();
        }
    });

    return closestPoint;
}

export const panIfRestricted = (map: google.maps.Map, boundsRestrictions: google.maps.LatLngBoundsLiteral[], defaultCenter: google.maps.LatLngLiteral) => {
    const center = map.getCenter()?.toJSON() || defaultCenter;
    if (!center) return;
    if (!isWithinRestrictions(center, boundsRestrictions)) {
        const newCenter = findClosestCenter(center, boundsRestrictions, defaultCenter);
        map.panTo(newCenter);
    }
}