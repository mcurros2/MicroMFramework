import type { MicroMError } from "../../client/MicroMError";

interface SelectionLabels {
    selectedRowsLabel: string;
    selectedRowLabel?: string;
}

export function getSelectedRowsLabel(count: number, labels?: SelectionLabels): string {
    if (count === 0) return '';
    return (count === 1 ? labels?.selectedRowLabel ?? labels?.selectedRowsLabel : labels?.selectedRowsLabel) ?? '';
}

export function getViewErrorMessage(error: MicroMError, fetchErrorLabel?: string): string {
    if (fetchErrorLabel !== undefined && !error.status && error.message === 'Failed to fetch') {
        return fetchErrorLabel;
    }
    return [error.status, error.message, error.statusMessage]
        .filter(value => value !== undefined && value !== null && value !== '')
        .join(' ');
}
