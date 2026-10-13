import type { BoxProps } from '@mantine/core';
import type { Value } from '../../client';
import type { EntityColumn } from '../../Entity';
import { EntityColumnFlags } from '../../Entity';
import { FormMode, MicroMWidthSizes } from './types';

export type FieldAutoFocus = 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean;

export interface FieldWidthProps {
    maxWidth?: keyof typeof MicroMWidthSizes;
    minWidth?: keyof typeof MicroMWidthSizes;
    maw?: BoxProps['maw'];
    miw?: BoxProps['miw'];
}

// UI resolution only: validation and normalization continue using their existing inputs.
export function resolveFieldReadOnly(column: EntityColumn<Value>, formMode: FormMode, readOnly?: boolean): boolean {
    return formMode === 'view' || (readOnly ?? (
        column.hasFlag(EntityColumnFlags.autoNum) ||
        (formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk))
    ));
}

export function resolveFieldAutoFocus(autoFocus: FieldAutoFocus | undefined, formMode: FormMode, loading?: boolean): boolean | undefined {
    if (autoFocus === 'autoFocusOnAdd') return formMode === 'add' ? true : undefined;
    if (autoFocus === 'autoFocusOnEdit') return loading === false && formMode !== 'add' ? true : undefined;
    return autoFocus;
}

export function resolveFieldWidths({ maw, miw, maxWidth, minWidth }: FieldWidthProps) {
    return {
        maw: maw ?? (maxWidth !== undefined && maxWidth !== 'auto' ? MicroMWidthSizes[maxWidth] : undefined),
        miw: miw ?? (minWidth !== undefined && minWidth !== 'auto' ? MicroMWidthSizes[minWidth] : undefined),
    };
}

