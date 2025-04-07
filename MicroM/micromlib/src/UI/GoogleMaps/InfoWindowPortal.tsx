import { createPortal } from "react-dom";

export interface InfoWindowPortalProps {
    container: Element | DocumentFragment,
    children: React.ReactNode,
}

export function InfoWindowPortal({ children, container }: InfoWindowPortalProps) {
    return createPortal(children, container);
}