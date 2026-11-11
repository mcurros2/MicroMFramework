import { Modal, ModalProps, useComponentDefaultProps } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import { useEffect } from "react";
import { MicroMClient, MicroMClientClaimTypes, MicroMToken, OperationStatus, TwoFactorLoginResult } from "../../client";
import { Login } from "./Login";

export interface LoginModalProps extends Omit<ModalProps, 'opened'> {
    client: MicroMClient,
    onLoggedIn: (claims?: Partial<MicroMClientClaimTypes>) => void,
    checkOnInit?: boolean
}

export const LoginModalDefaultProps: Partial<LoginModalProps> = {
    title: "Sign in",
    checkOnInit: true,
    centered: true,
    withCloseButton: false,
    closeOnClickOutside: false,
    closeOnEscape: false,
    trapFocus: true
}

export function LoginModal(props: LoginModalProps) {
    const { client, onClose, onLoggedIn, checkOnInit, ...others } = useComponentDefaultProps('LoginModal', LoginModalDefaultProps, props);
    const [opened, { open, close }] = useDisclosure(false);

    const statusCompletedHandler = (status: OperationStatus<MicroMToken | TwoFactorLoginResult>) => {
        if (!status.error && !status.loading) {
            if (status.data instanceof MicroMToken) {
                onLoggedIn(status.data?.claims);
            }
            close();
        }
    }

    const closeHandler = () => {
        close();
        if (onClose) onClose();
    }

    useEffect(() => {

        if (checkOnInit) {
            async function check() {
                const result = await client.isLoggedInLocal();
                if (!result) {
                    open();
                }
                else {
                    onLoggedIn(client.LOGGED_IN_USER);
                }
            }
            check();
        }
        else {
            open();
        }

    }, []);

    return (
        <Modal opened={opened} onClose={closeHandler} {...others}>
            <Login client={client} onStatusCompleted={statusCompletedHandler} />
        </Modal>
    )
}
