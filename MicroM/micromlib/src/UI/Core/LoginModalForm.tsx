import { Modal, OverlayProps, TransitionProps, useComponentDefaultProps } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import { useEffect } from "react";
import { MicroMClient, MicroMClientClaimTypes, MicroMToken, OperationStatus } from "../../client";
import { Login } from "./Login";

export interface LoginModalFormProps {
    client: MicroMClient,
    title?: string,
    onClose?: () => void,
    onLoggedIn: (claims?: Partial<MicroMClientClaimTypes>) => void,
    openState?: boolean,
    withCloseButton?: boolean,
    closeOnClickOutside?: boolean,
    closeOnEscape?: boolean,
    overlayProps?: OverlayProps,
    transitionProps?: Partial<Omit<TransitionProps, 'mounted'>>
}

export const LoginModalFormDefaultProps: Partial<LoginModalFormProps> = {
    title: "Sign in",
    withCloseButton: true,
    closeOnEscape: true,
}

export function LoginModalForm(props: LoginModalFormProps) {
    const {
        client, title, onClose, onLoggedIn, openState, withCloseButton, closeOnClickOutside, closeOnEscape, overlayProps,
        transitionProps
    } = useComponentDefaultProps('LoginModalForm', LoginModalFormDefaultProps, props);
    const [opened, { open, close }] = useDisclosure(false);

    const statusCompletedHandler = (status: OperationStatus<MicroMToken>) => {
        if (!status.error && !status.loading) {
            onLoggedIn(status.data?.claims);
            close();
        }
    }

    const closeHandler = () => {
        close();
        if (onClose) onClose();
    }

    useEffect(() => {

        if (openState) {
            open();
        }
        else {
            close();
        }

    }, [close, open, openState]);

    return (
        <Modal
            trapFocus
            opened={opened}
            onClose={closeHandler}
            title={title}
            centered
            withCloseButton={withCloseButton}
            closeOnClickOutside={closeOnClickOutside}
            closeOnEscape={closeOnEscape}
            overlayProps={overlayProps}
            transitionProps={transitionProps}
        >
            <Login client={client} onStatusCompleted={statusCompletedHandler} />
        </Modal>
    )
}