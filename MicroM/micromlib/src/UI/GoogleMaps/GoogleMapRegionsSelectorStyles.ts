
export interface GoogleMapZoomFeatureStyles {
    styleDefault?: google.maps.FeatureStyleOptions,
    styleClicked?: google.maps.FeatureStyleOptions,
    styleMouseMove?: google.maps.FeatureStyleOptions,
}

export const GoogleMapLowZoomStyleDefaultProps: Partial<GoogleMapZoomFeatureStyles> = {
    styleDefault: {
        strokeColor: "#73383e",
        strokeOpacity: 0.1,
        strokeWeight: 2.0,
        fillColor: "white",
        fillOpacity: 0.1, // Polygons must be visible to be clickable
    },
    styleClicked: {
        strokeColor: "#73383e",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "#73383e",
        fillOpacity: 0.5,
    },
    styleMouseMove: {
        strokeColor: "#73383e",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "white",
        fillOpacity: 0.1,
    },
};

export const GoogleMapMediumZoomStyleDefaultProps: Partial<GoogleMapZoomFeatureStyles> = {
    styleDefault: {
        strokeColor: "#5d503f",
        strokeOpacity: 0.1,
        strokeWeight: 2.0,
        fillColor: "white",
        fillOpacity: 0.1, // Polygons must be visible to be clickable
    },
    styleClicked: {
        strokeColor: "#5d503f",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "#5d503f",
        fillOpacity: 0.5,
    },
    styleMouseMove: {
        strokeColor: "#5d503f",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "white",
        fillOpacity: 0.1,
    },
};

export const GoogleMapHighZoomStyleDefaultProps: Partial<GoogleMapZoomFeatureStyles> = {
    styleDefault: {
        strokeColor: "#22333b",
        strokeOpacity: 0.1,
        strokeWeight: 2.0,
        fillColor: "white",
        fillOpacity: 0.1, // Polygons must be visible to be clickable
    },
    styleClicked: {
        strokeColor: "#22333b",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "#22333b",
        fillOpacity: 0.5,
    },
    styleMouseMove: {
        strokeColor: "#22333b",
        strokeOpacity: 1.0,
        strokeWeight: 4.0,
        fillColor: "white",
        fillOpacity: 0.1,
    },
};


