import { RefObject, createContext, useContext } from "react";

export interface GoogleMapContextType {
    map: google.maps.Map | null,
    renderMarkers: () => React.ReactNode,
    setInfoWindowContent?: (content: React.ReactNode) => void,
    infoWindowRef: RefObject<google.maps.InfoWindow>,
    infoWindowContentRef?: RefObject<HTMLDivElement>,
}

export const GoogleMapContext = createContext<GoogleMapContextType | null>(null);

export function useGoogleMap() {
    const context = useContext(GoogleMapContext);

    if (context === null) {
        throw new Error('useGoogleMap must be used within a GoogleMapContext provider');
    }

    return context;
}