import { Anchor, Button, Checkbox, Group, PasswordInput, Text, TextInput, useComponentDefaultProps } from "@mantine/core";
import { useForm } from "@mantine/form";
import { IconMailCheck } from "@tabler/icons-react";
import { useCallback, useEffect, useState } from "react";
import { MicroMClient, MicroMToken, OperationStatus, StatusCompletedHandler, toMicroMError } from "../../client";
import { AlertError, FakeProgressBar, RecoverPasswordEmail, useModal } from "../Core";

export interface LoginOptions {
    client: MicroMClient,
    onStatusCompleted: StatusCompletedHandler<MicroMToken>,
    userLabel?: string,
    userPlaceholder?: string,
    passwordLabel?: string,
    passwordPlaceholder?: string,
    rememberLabel?: string,
    forgotLabel?: string,
    signInButtonLabel?: string,
    loginErrorMessage?: string,
    confirmRecoveryEmailTitle?: string,
}

export const LoginDefaultProps: Partial<LoginOptions> = {
    userLabel: "User",
    userPlaceholder: "your@email.com",
    passwordLabel: "Password",
    passwordPlaceholder: "Your password",
    rememberLabel: "Remember me",
    forgotLabel: "Forgot password?",
    signInButtonLabel: "Sign in",
    loginErrorMessage: "Unknown user name or bad password",
    confirmRecoveryEmailTitle: "Recovery email",
}

export interface LoginValues { user: string, password: string, server: string, rememberme: boolean }

export function Login(props: LoginOptions) {
    const {
        client, onStatusCompleted, userLabel, userPlaceholder, passwordLabel, passwordPlaceholder,
        rememberLabel, forgotLabel, signInButtonLabel, loginErrorMessage, confirmRecoveryEmailTitle,
    } = useComponentDefaultProps('Login', LoginDefaultProps, props);

    const modal = useModal();

    const form = useForm<LoginValues>(
        {
            initialValues: {
                user: '',
                password: '',
                server: '',
                rememberme: false
            }
        });

    const [status, setStatus] = useState<OperationStatus<MicroMToken>>();

    const handleClick = useCallback(async (values: LoginValues) => {
        setStatus({ loading: true });
        try {
            const data = await client.login(values.user, values.password, values.rememberme);
            const new_status = { data: data };
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
        catch (e) {
            const new_status = { error: toMicroMError(e) };
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
    }, [client, onStatusCompleted]);

    const handleForgotPasswordClick = useCallback(async () => {
        await modal.open({
            modalProps: {
                title: <Group><IconMailCheck size="1.5rem" stroke="1.5" /><Text fw="700">{confirmRecoveryEmailTitle}</Text></Group>,
            },
            content: <RecoverPasswordEmail client={client} />
        });

    }, [client, confirmRecoveryEmailTitle, modal]);

    useEffect(() => {
        const getRememberUser = async () => {
            const result = await client.getRememberUser();
            form.setFieldValue('user', result ?? '');
            if (result) {
                form.setFieldValue('rememberme', true);
            }
        };
        getRememberUser();
    }, [client]);

    return (
        <>
            <form key="loginForm" onSubmit={form.onSubmit((values) => handleClick(values))}>
                {status?.loading && <FakeProgressBar />}
                <TextInput label={userLabel} placeholder={userPlaceholder} required data-autofocus disabled={status?.loading} {...form.getInputProps('user')} />
                <PasswordInput label={passwordLabel} placeholder={passwordPlaceholder} required mt="md" disabled={status?.loading}  {...form.getInputProps('password')} />
                <Group position="apart" mt="lg">
                    <Checkbox label={rememberLabel} {...form.getInputProps('rememberme', { type: 'checkbox' })} />
                    <Anchor component="button" size="sm" onClick={handleForgotPasswordClick}>
                        {forgotLabel}
                    </Anchor>
                </Group>
                <Button key="login" fullWidth mt="xl" disabled={status?.loading} type="submit">
                    {signInButtonLabel}
                </Button>
            </form>
            <AlertError mt="xs" hidden={status?.error === undefined}>{
                (status?.error !== undefined && status.error.name === 'validation') ? status.error.message : loginErrorMessage
            }</AlertError>
        </>
    );
}