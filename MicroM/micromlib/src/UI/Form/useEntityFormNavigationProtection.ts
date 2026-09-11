import type { ReactNode } from "react";
import { useConfirmNavigation } from "../Router/useConfirmNavigation";
import type { UseEntityFormReturnType } from "./useEntityForm";

export interface EntityFormNavigationPresentation {
    showOK?: boolean,
    showCancel?: boolean,
    buttons?: ReactNode,
}

/* Installs an entity form's navigation guard using the buttons rendered by its form shell. */
export function useEntityFormNavigationProtection(
    formAPI: UseEntityFormReturnType,
    { showCancel, showOK, buttons }: EntityFormNavigationPresentation,
): void {
    useConfirmNavigation({
        ...formAPI.navigationGuard,
        /* Hidden standard buttons without custom buttons make `confirm` behave as `disabled`. */
        mode:
            (formAPI.navigationGuard.mode === 'confirm' && showCancel === false && showOK === false && !buttons)
            ? 'disabled'
            : formAPI.navigationGuard.mode,
    });
}
