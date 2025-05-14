import { ActionIcon, Group, MantineNumberSize, Modal, ModalBaseOverlayProps, Skeleton } from '@mantine/core';
import { randomId, useViewportSize } from '@mantine/hooks';
import { ModalSettings } from '@mantine/modals/lib/context';
import { IconArrowsDiagonal, IconArrowsDiagonalMinimize2 } from '@tabler/icons-react';
import { PropsWithChildren, ReactNode, createContext, useCallback, useContext, useState } from 'react';
import { isPromise } from '../../Entity';

export const ModalsManagerDefaultProps = {
    closeLabel: 'Close',
    fullscreenLabel: 'Fullscreen',
    minimizeLabel: 'Minimize',
    toggleLabel: 'Toggle',
    FullScreenIcon: IconArrowsDiagonal,
    RestoreScreeSizeIcon: IconArrowsDiagonalMinimize2,
    withCloseButton: true,
}

export type MicroMModalSize = MantineNumberSize | 'fullscreen';



export type MicroMModalSettings = Partial<Omit<ModalSettings, 'size'>> & {
    size?: MicroMModalSize,
    withFullscreenButton?: boolean,
};



export interface ModalOpenProps {
    content: ReactNode | Promise<ReactNode>,
    modalProps: MicroMModalSettings,
    onClosed?: () => void,
    focusOnClosed?: HTMLElement
}

export interface ModalContextType {
    open: (props: ModalOpenProps, onClosed?: () => void) => Promise<void>;
    close: () => Promise<void>;
}

export interface ModalType {
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

const ModalContext = createContext<ModalContextType | null>(null);

export const ModalsManager = ({ modalProps, animationDuration, children }: ModalsManagerProps) => {
    const [modals, setModals] = useState<ModalType[]>([]);
    const [isClosing, setIsClosing] = useState(false);

    const { width: viewportWidth } = useViewportSize();

    const transparentOverlay: ModalBaseOverlayProps = {
        ...modalProps.overlayProps,
        opacity: 0,
        blur: 0,
    }

    const open = useCallback(async ({ content, modalProps, onClosed, focusOnClosed }: ModalOpenProps): Promise<void> => {
        if (isClosing) {
            await new Promise(resolve => {
                const checkInterval = setInterval(() => {
                    if (!isClosing) {
                        clearInterval(checkInterval);
                        resolve(null);
                    }
                }, 100);
            });
        }

        const modal_id = randomId(); // Generate ID upfront

        // try to get the last focused event if none specified
        if (!focusOnClosed) {
            focusOnClosed = document.activeElement as HTMLElement;
        }

        // Default for closeButton
        if (modalProps.withCloseButton === undefined) {
            modalProps.withCloseButton = ModalsManagerDefaultProps.withCloseButton;
        }

        // If content is a Promise
        if (isPromise<ReactNode>(content)) {
            content.then(resolvedContent => {
                setModals((prev) => {
                    return prev.map(m =>
                        m.id === modal_id ? { ...m, resolvedContent } : m
                    );
                });
            });

            setModals(prevModals => [
                ...prevModals,
                {
                    originalContent: content,
                    opened: true,
                    id: modal_id,
                    props: modalProps,
                    onClosed,
                    focusOnClosed,
                    initialSize: modalProps.size,
                    withFullscreenButton: modalProps.withFullscreenButton,
                }
            ]);
        }
        // If content is not a Promise
        else {
            setModals(prevModals => [
                ...prevModals,
                {
                    originalContent: content,
                    resolvedContent: content, // Directly setting resolved content for non-promise content
                    opened: true,
                    id: modal_id,
                    props: modalProps,
                    onClosed,
                    focusOnClosed,
                    initialSize: modalProps.size,
                    withFullscreenButton: modalProps.withFullscreenButton,
                }
            ]);
        }
    }, [isClosing]);

    const getModalSize = useCallback((size?: MicroMModalSize): { size?: MantineNumberSize, fullscreen?: boolean } => {
        if (size === 'fullscreen' || size === '100%' || (viewportWidth < 768 && (['md', 'lg', 'xl', 'fullscreen'] as MicroMModalSize[]).includes(size ?? ''))) return { fullscreen: true, size: undefined };

        let new_size = size;
        if (size && NEW_SIZES[size]) {
            new_size = NEW_SIZES[size] as MantineNumberSize;
        }
        return { size: new_size, fullscreen: undefined };
    }, [viewportWidth]);

    const close = useCallback(() => {
        let timer: ReturnType<typeof setTimeout> | null = null;
        return new Promise<void>((resolve) => {
            setModals((prevModals) => {
                if (prevModals.length > 0) {
                    const newModals = [...prevModals];
                    newModals[newModals.length - 1].opened = false; // Close the last modal
                    setIsClosing(true);

                    // After a delay, remove the modal completely
                    if (!timer) {
                        timer = setTimeout(() => {
                            const closedModal = newModals.pop();
                            setModals(newModals);
                            if (closedModal?.onClosed) {
                                closedModal.onClosed();
                            }
                            setIsClosing(false);
                            if (closedModal?.focusOnClosed) {
                                closedModal.focusOnClosed.focus();
                            }
                            resolve();
                        }, animationDuration); // Adjust this delay to match the closing animation duration
                    }

                    return newModals;
                }
                return [];
            });
        });
    }, [animationDuration]);

    const IconFullscreen = ModalsManagerDefaultProps.FullScreenIcon;
    const IconRestore = ModalsManagerDefaultProps.RestoreScreeSizeIcon;

    return (
        <ModalContext.Provider value={{ open, close }}>
            {children}
            {
                modals.map((modal, index) => {
                    const computedSizes = getModalSize(modal.props.size);

                    return (
                        <Modal.Root
                            key={modal.id}
                            opened={modal.opened}
                            onClose={async () => { await close(); }}
                            size={computedSizes.size}
                            fullScreen={computedSizes.fullscreen}
                            zIndex={(index + 1) * 5000}
                            returnFocus={false}
                            trapFocus
                            closeOnClickOutside={modal.props.closeOnClickOutside ?? false}
                            closeOnEscape={modal.props.closeOnEscape ?? true}
                        >
                            <Modal.Overlay {...((index === modals.length - 1) ? modalProps.overlayProps : transparentOverlay)} />

                            <Modal.Content>
                                <Modal.Header>
                                    <Modal.Title>
                                        {modal.props.title}
                                    </Modal.Title>
                                    <Group position="right">
                                        {modal.withFullscreenButton &&
                                            <ActionIcon
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    setModals((prev) =>
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
                                    {modal.resolvedContent ?
                                        modal.resolvedContent :
                                        (isPromise<ReactNode>(modal.originalContent) ? <Skeleton /> : modal.originalContent)}
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
