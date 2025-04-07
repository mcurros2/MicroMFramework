import { InfoWindowPortal } from "./InfoWindowPortal";

export interface GoogleMapsInfoWindowProps {
    container: Element | DocumentFragment,
    infoWindowContent: React.ReactNode
}

export function GoogleMapsInfoWindow({ container, infoWindowContent }: GoogleMapsInfoWindowProps) {

    return (
        <InfoWindowPortal container={container}>
            {infoWindowContent}
        </InfoWindowPortal>
    )
}