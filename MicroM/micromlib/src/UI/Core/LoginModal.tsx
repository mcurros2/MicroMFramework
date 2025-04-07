import { Modal, useComponentDefaultProps } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import { useEffect } from "react";
import { MicroMClient, MicroMClientClaimTypes, MicroMToken, OperationStatus } from "../../client";
import { Login } from "./Login";

export interface LoginModalProps {
    client: MicroMClient,
    title?: string,
    onClose?: () => void,
    onLoggedIn: (claims?: Partial<MicroMClientClaimTypes>) => void,
    checkOnInit?: boolean
}

export const LoginModalDefaultProps: Partial<LoginModalProps> = {
    title: "Sign in",
    checkOnInit: true
}

export function LoginModal(props: LoginModalProps) {
    const { client, title, onClose, onLoggedIn, checkOnInit } = useComponentDefaultProps('LoginModal', LoginModalDefaultProps, props);
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
        <Modal trapFocus opened={opened} onClose={closeHandler} title={title} centered withCloseButton={false} closeOnClickOutside={false} closeOnEscape={false}>
            <Login client={client} onStatusCompleted={statusCompletedHandler} />
        </Modal>
    )
}