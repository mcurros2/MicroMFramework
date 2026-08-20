import { Group, Text } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useCallback, useEffect, useRef } from 'react';
import { useModal } from '../Core/ModalsManager';
import { ConfirmLeaveStaySave, ConfirmLeaveStaySaveDefaultProps, ConfirmLeaveStaySaveResult } from './ConfirmLeaveStaySave';
import type { NavigationProtectionMode } from './NavigationGuards';
import { registerLocalNavigationGuard } from './NavigationGuards';

export type { NavigationProtectionMode } from './NavigationGuards';

export type ConfirmNavigationType = 'local' | 'remote';

export interface UseConfirmNavigationOptions {
    mode?: NavigationProtectionMode,
    hasUnsavedChanges: () => boolean,
    onSave: (navigationType: ConfirmNavigationType) => Promise<boolean>,
}

export function useConfirmNavigation({ mode, hasUnsavedChanges, onSave }: UseConfirmNavigationOptions): void {
    const modals = useModal();

    const hasUnsavedChangesRef = useRef(hasUnsavedChanges);
    const onSaveRef = useRef(onSave);
    const externalNavigationBypassRef = useRef(false);
    const confirmationPendingRef = useRef(false);
    const cancelPendingConfirmationRef = useRef<(() => void) | null>(null);
    const externalNavigationPendingRef = useRef(false);

    useEffect(() => {
        hasUnsavedChangesRef.current = hasUnsavedChanges;
        onSaveRef.current = onSave;
    }, [hasUnsavedChanges, onSave]);

    const requestNavigationConfirmation = useCallback((navigationType: ConfirmNavigationType): Promise<boolean> => {
        if (confirmationPendingRef.current) return Promise.resolve(false);

        confirmationPendingRef.current = true;

        return new Promise<boolean>((resolve) => {
            let resultSelected = false;

            const settle = async (canNavigate: boolean) => {
                if (resultSelected) return;

                resultSelected = true;
                cancelPendingConfirmationRef.current = null;
                await modals.close();
                confirmationPendingRef.current = false;
                resolve(canNavigate);
            };

            const handleResult = async (result: ConfirmLeaveStaySaveResult) => {
                if (result === 'save') {
                    let saved = false;
                    try {
                        saved = await onSaveRef.current(navigationType);
                    }
                    catch {
                        saved = false;
                    }
                    await settle(saved);
                    return;
                }

                await settle(result === 'leave');
            };

            cancelPendingConfirmationRef.current = () => {
                if (resultSelected) return;

                resultSelected = true;
                cancelPendingConfirmationRef.current = null;
                confirmationPendingRef.current = false;
                void modals.close().finally(() => resolve(false));
            };

            void modals.open({
                content: <ConfirmLeaveStaySave onResult={handleResult} />,
                modalProps: {
                    title: <Group spacing="xs"><IconAlertTriangle size="1.25rem" /><Text fw={700}>{ConfirmLeaveStaySaveDefaultProps.title}</Text></Group>,
                    size: 'md',
                    withCloseButton: false,
                    withFullscreenButton: false,
                    closeOnClickOutside: false,
                    closeOnEscape: false,
                },
                onClosed: () => {
                    if (resultSelected) return;

                    resultSelected = true;
                    cancelPendingConfirmationRef.current = null;
                    confirmationPendingRef.current = false;
                    resolve(false);
                },
            });
        });
    }, [modals]);

    useEffect(() => {
        return () => cancelPendingConfirmationRef.current?.();
    }, []);

    useEffect(() => {
        if (!mode) return;

        return registerLocalNavigationGuard(() => {
            if (!hasUnsavedChangesRef.current()) return true;
            if (mode === 'save') return onSaveRef.current('local');
            return requestNavigationConfirmation('local');
        });
    }, [mode, requestNavigationConfirmation]);

    useEffect(() => {
        if (!mode) return;

        const handleBeforeUnload = (event: BeforeUnloadEvent) => {
            if (externalNavigationBypassRef.current || !hasUnsavedChangesRef.current()) return;

            if (mode === 'confirm') {
                event.preventDefault();
                event.returnValue = '';
                return;
            }

            void onSaveRef.current('remote');
        };

        window.addEventListener('beforeunload', handleBeforeUnload);

        return () => {
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [mode]);

    useEffect(() => {
        if (!mode) return;

        const handleDocumentClick = (event: MouseEvent) => {
            if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            if (!(event.target instanceof Element)) return;

            const anchor = event.target.closest<HTMLAnchorElement>('a[href]');
            if (!anchor || anchor.download || (anchor.target && anchor.target.toLowerCase() !== '_self')) return;
            if (!hasUnsavedChangesRef.current()) return;

            const destination = new URL(anchor.href, window.location.href);
            if (destination.protocol !== 'http:' && destination.protocol !== 'https:') return;

            const currentUrl = new URL(window.location.href);
            const isSameDocument = destination.origin === currentUrl.origin
                && destination.pathname === currentUrl.pathname
                && destination.search === currentUrl.search;

            if (destination.href === currentUrl.href || (isSameDocument && destination.hash !== currentUrl.hash)) return;

            event.preventDefault();
            if (externalNavigationPendingRef.current) return;

            externalNavigationPendingRef.current = true;
            void (async () => {
                const canNavigate = mode === 'save'
                    ? await onSaveRef.current('remote')
                    : await requestNavigationConfirmation('remote');

                if (!canNavigate) {
                    externalNavigationPendingRef.current = false;
                    return;
                }

                externalNavigationBypassRef.current = true;
                window.location.assign(destination.href);
            })();
        };

        document.addEventListener('click', handleDocumentClick);
        return () => document.removeEventListener('click', handleDocumentClick);
    }, [mode, requestNavigationConfirmation]);
}
