import { Text, useComponentDefaultProps } from "@mantine/core";
import { ModalSettings } from '@mantine/modals/lib/context';
import { MicroMClient, MicroMToken, OperationStatus } from "../../client";
import { Login, useModal } from "../Core";

export interface useLoginType {
    login: () => void
}

export interface useLoginOptions {
    client: MicroMClient,
    modalProps?: ModalSettings,
    onClose?: () => void,
    title?: string
    //isAdmin?: boolean
}

export const UseLoginDefaultProps: Partial<useLoginOptions> = {
    title: "Sign-in"
}
export function useLogin(props: useLoginOptions) {
    const { client, modalProps, title, onClose } = useComponentDefaultProps('useLogin', UseLoginDefaultProps, props);
    const modals = useModal();

    const statusCompletedHandler = async (status: OperationStatus<MicroMToken>) => {
        if (!status.error && !status.loading) {
            await modals.close();
        }
    }

    const login = async () => {
        await modals.open(
            {
                content: <Login client={client} onStatusCompleted={statusCompletedHandler} />,
                modalProps: {
                    ...modalProps,
                    title: <Text fw="700">{title}</Text>,
                },
                onClosed: () => { if (onClose) onClose(); }

            });
    }

    return login;
}