import { ActionIcon, Group, MantineNumberSize, Modal, ModalBaseOverlayProps, Skeleton } from '@mantine/core';
import { randomId, useViewportSize } from '@mantine/hooks';
import { ModalSettings } from '@mantine/modals/lib/context';
import { IconArrowsDiagonal, IconArrowsDiagonalMinimize2 } from '@tabler/icons-react';
import { createContext, PropsWithChildren, ReactNode, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { isPromise } from '../../Entity';
import { ModalHistory, registerModalNavigator } from '../Router/ModalNavigation';
import { canNavigateLocally } from '../Router/NavigationGuards';

export const ModalsManagerDefaultProps = {
    closeLabel: 'Close',
    fullscreenLabel: 'Fullscreen',
    minimizeLabel: 'Minimize',
    toggleLabel: 'Toggle',
    FullScreenIcon: IconArrowsDiagonal,
    RestoreScreeSizeIcon: IconArrowsDiagonalMinimize2,
    withCloseButton: true,
    withFullscreenButton: true
}

export type MicroMModalSize = MantineNumberSize | 'fullscreen';



export type MicroMModalSettings = Partial<Omit<ModalSettings, 'size'>> & {
    size?: MicroMModalSize,
    withFullscreenButton?: boolean,
};



export interface ModalOpenProps {
    /** Optional stable identifier for targeted programmatic closes. */
    id?: string,
    /** Internal confirmation dialogs use false; normal modals participate in Back. */
    history?: boolean,
    content: ReactNode | Promise<ReactNode>,
    modalProps: MicroMModalSettings,
    onClosed?: () => void,
    focusOnClosed?: HTMLElement
}

export interface ModalContextType {
    open: (props: ModalOpenProps, onClosed?: () => void) => Promise<void>;
    close: (id?: string) => Promise<void>;
}

export interface ModalType {
    history?: boolean,
    originalContent: ReactNode | Promise<ReactNode>,
    resolvedContent?: ReactNode, // Resolved content to be rendered
    props: ModalSettings,
    id: string,
    opened: boolean,
    onClosed?: () => void,
    focusOnClosed?: HTMLElement,
    initialSize?: MicroMModalSize,
    withFullscreenButton?: boolean,
}

export interface ModalsManagerProps extends PropsWithChildren {
    modalProps: MicroMModalSettings,
    /** This delay in milliseconds will be used to wait for the modal to be closed and fire the onClosed() event */
    animationDuration: number
}


const NEW_SIZES: Record<string, string> = {
    xs: '20%',
    sm: '30%',
    md: '40%',
    lg: '60%',
    xl: '80%',
    fullscreen: '100%'
}

const ModalScopeContext = createContext<string | undefined>(undefined);
export const useModalScope = () => useContext(ModalScopeContext);

const ModalContext = createContext<ModalContextType | null>(null);

export const ModalsManager = ({ modalProps, animationDuration, children }: ModalsManagerProps) => {
    const [modals, setModals] = useState<ModalType[]>([]);

    const modalsRef = useRef<ModalType[]>([]);
    const closingRef = useRef(new Map<string, Promise<void>>());
    const requestPendingRef = useRef(false);
    const historyRef = useRef<ModalHistory>();
    const requestCloseRef = useRef<() => Promise<void>>(async () => { });
    const mountedRef = useRef(true);

    const updateModals = useCallback((update: (previous: ModalType[]) => ModalType[]) => {
        modalsRef.current = update(modalsRef.current);
        if (mountedRef.current) setModals(modalsRef.current);
    }, []);

    const { width: viewportWidth } = useViewportSize();

    const transparentOverlay: ModalBaseOverlayProps = {
        ...modalProps.overlayProps,
        opacity: 0,
        blur: 0,
    }

    const open = useCallback(async ({ content, modalProps, onClosed, focusOnClosed, id, history = true }: ModalOpenProps, fallbackOnClosed?: () => void): Promise<void> => {
        if (!mountedRef.current) return;

        const modalId = id ?? randomId();

        if (modalsRef.current.some(modal => modal.id === modalId)) throw new Error(`Modal ${modalId} is already open`);

        historyRef.current ??= new ModalHistory(() => requestCloseRef.current());
        if (history) await historyRef.current.open(modalId);

        if (!mountedRef.current) return;

        const props = {
            ...modalProps,
            withCloseButton: modalProps.withCloseButton ?? ModalsManagerDefaultProps.withCloseButton,
            withFullscreenButton: modalProps.withFullscreenButton ?? ModalsManagerDefaultProps.withFullscreenButton,
        };

        updateModals(previous => [...previous, {
            id: modalId, history, originalContent: content,
            resolvedContent: isPromise<ReactNode>(content) ? undefined : content,
            opened: true, props, onClosed: onClosed ?? fallbackOnClosed,
            focusOnClosed: focusOnClosed ?? document.activeElement as HTMLElement,
            initialSize: props.size, withFullscreenButton: props.withFullscreenButton,
        }]);

        if (isPromise<ReactNode>(content)) {
            void content.then(resolvedContent => {
                updateModals(previous => previous.map(modal => modal.id === modalId ? { ...modal, resolvedContent } : modal));
            }).catch(error => { console.error('Could not load modal content', error); void closeRef.current(modalId); });
        }
    }, [updateModals]);

    const getModalSize = useCallback((size?: MicroMModalSize): { size?: MantineNumberSize, fullscreen?: boolean } => {
        if (size === 'fullscreen' || size === '100%' || (viewportWidth < 768 && (['xs', 'sm', 'md', 'lg', 'xl', 'fullscreen'] as MicroMModalSize[]).includes(size ?? ''))) return { fullscreen: true, size: undefined };

        let new_size = size;
        if (size && NEW_SIZES[size]) {
            new_size = NEW_SIZES[size] as MantineNumberSize;
        }

        return { size: new_size, fullscreen: undefined };
    }, [viewportWidth]);

    const close = useCallback((id?: string): Promise<void> => {
        const modal = id ? modalsRef.current.find(item => item.id === id) : modalsRef.current[modalsRef.current.length - 1];

        if (!modal) return Promise.resolve();

        const pending = closingRef.current.get(modal.id);
        if (pending) return pending;

        const closing = (async () => {
            updateModals(previous => previous.map(item => item.id === modal.id ? { ...item, opened: false } : item));

            if (modal.history) await historyRef.current?.close(modal.id);

            await new Promise(resolve => setTimeout(resolve, animationDuration));

            updateModals(previous => previous.filter(item => item.id !== modal.id));

            if (mountedRef.current) {
                modal.onClosed?.();
                if (modal.focusOnClosed?.isConnected) modal.focusOnClosed.focus();
            }
        })().finally(() => { closingRef.current.delete(modal.id); });
        closingRef.current.set(modal.id, closing);
        return closing;
    }, [animationDuration, updateModals]);

    const closeRef = useRef(close);
    closeRef.current = close;

    const requestClose = useCallback(async (id?: string): Promise<void> => {
        const top = modalsRef.current[modalsRef.current.length - 1];

        if (!top || !top.opened || top.history === false || (id && top.id !== id) || requestPendingRef.current) return;

        requestPendingRef.current = true;

        try {
            const route = window.location.hash.slice(1);
            if (await canNavigateLocally({ currentRoute: route, nextRoute: route }, top.id)) await close(top.id);
        }
        catch (error) { console.error('Modal close was cancelled', error); }
        finally { requestPendingRef.current = false; }

    }, [close]);

    requestCloseRef.current = requestClose;

    useEffect(() => {
        mountedRef.current = true;
        historyRef.current ??= new ModalHistory(() => requestCloseRef.current());

        const unregister = registerModalNavigator({
            hasModals: () => modalsRef.current.length > 0,
            requestClose: () => requestCloseRef.current(),
            restoreRoute: async () => { await historyRef.current?.restoreRoute(); },
        });

        return () => {
            mountedRef.current = false;
            unregister();
            historyRef.current?.dispose();
            historyRef.current = undefined;
        };
    }, []);

    const contextValue = useMemo(() => ({ open, close }), [open, close]);

    const IconFullscreen = ModalsManagerDefaultProps.FullScreenIcon;
    const IconRestore = ModalsManagerDefaultProps.RestoreScreeSizeIcon;

    return (
        <ModalContext.Provider value={contextValue}>
            {children}
            {
                modals.map((modal, index) => {
                    const computedSizes = getModalSize(modal.props.size);
                    const mobileSize = (viewportWidth < 768 && (['md', 'lg', 'xl', 'fullscreen'] as MicroMModalSize[]).includes(modal.initialSize ?? ''));

                    return (
                        <Modal.Root
                            key={modal.id}
                            opened={modal.opened}
                            onClose={() => { void requestClose(modal.id); }}
                            size={computedSizes.size}
                            fullScreen={computedSizes.fullscreen}
                            zIndex={(index + 1) * 5000}
                            returnFocus={false}
                            trapFocus
                            closeOnClickOutside={index === modals.length - 1 && (modal.props.closeOnClickOutside ?? false)}
                            closeOnEscape={index === modals.length - 1 && (modal.props.closeOnEscape ?? true)}
                            transitionProps={modal.props.transitionProps}
                        >
                            <Modal.Overlay {...((index === modals.length - 1) ? modalProps.overlayProps : transparentOverlay)} />

                            <Modal.Content
                                sx={{
                                    height: computedSizes.fullscreen ? '100dvh' : undefined,
                                }}
                            >
                                <Modal.Header>
                                    <Modal.Title>
                                        {modal.props.title}
                                    </Modal.Title>
                                    <Group position="right">
                                        {modal.withFullscreenButton && mobileSize === false &&
                                            <ActionIcon
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    updateModals((prev) =>
                                                        prev.map((m, i) => {
                                                            if (i !== index) return m;
                                                            const original = m.initialSize ?? 'lg';
                                                            const currentSize = m.props.size;
                                                            const newSize =
                                                                (currentSize === 'fullscreen' || currentSize === '100%')
                                                                    ? (original === 'fullscreen' || original === '100%')
                                                                        ? 'lg'
                                                                        : original
                                                                    : 'fullscreen';

                                                            return {
                                                                ...m,
                                                                props: { ...m.props, size: newSize },
                                                            };
                                                        })
                                                    );
                                                }}
                                                variant="subtle"
                                                size="sm"
                                                title={`${ModalsManagerDefaultProps.toggleLabel} ${ModalsManagerDefaultProps.fullscreenLabel}`}
                                            >
                                                {modal.props.size === 'fullscreen' ? (
                                                    <IconRestore size="1rem" />
                                                ) : (
                                                    <IconFullscreen size="1rem" />
                                                )}
                                            </ActionIcon>
                                        }
                                        {modal.props.withCloseButton &&
                                            <Modal.CloseButton title={ModalsManagerDefaultProps.closeLabel} />
                                        }
                                    </Group>
                                </Modal.Header>

                                <Modal.Body style={{
                                    paddingBottom: 'calc(env(safe-area-inset-bottom, 0px) + 1rem)'
                                }}>
                                    <ModalScopeContext.Provider value={modal.id}>
                                        {modal.resolvedContent ?
                                            modal.resolvedContent :
                                            (isPromise<ReactNode>(modal.originalContent) ? <Skeleton /> : modal.originalContent)}
                                    </ModalScopeContext.Provider>
                                </Modal.Body>
                            </Modal.Content>
                        </Modal.Root>
                    );
                })
            }
        </ModalContext.Provider>
    );
};

export const useModal = (): ModalContextType => {
    const context = useContext(ModalContext);
    if (context === null) {
        throw new Error("useModal must be used within a ModalProvider");
    }
    return context;
};
