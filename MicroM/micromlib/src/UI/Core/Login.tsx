import { Anchor, Button, Checkbox, Group, Image, PasswordInput, PinInput, Stack, Text, TextInput, useComponentDefaultProps } from "@mantine/core";
import { useForm } from "@mantine/form";
import { IconMailCheck } from "@tabler/icons-react";
import { useCallback, useEffect, useState } from "react";
import { MicroMClient, MicroMToken, OperationStatus, StatusCompletedHandler, toMicroMError, TwoFactorLoginResult } from "../../client";
import { AlertError, FakeProgressBar, RecoverPasswordEmail, useModal } from "../Core";
import { TotpAuthenticatorsManagement } from "./TotpAuthenticatorsManagement";

export interface LoginOptions {
    client: MicroMClient,
    onStatusCompleted: StatusCompletedHandler<MicroMToken | TwoFactorLoginResult>,
    userLabel?: string,
    userPlaceholder?: string,
    passwordLabel?: string,
    passwordPlaceholder?: string,
    rememberLabel?: string,
    forgotLabel?: string,
    signInButtonLabel?: string,
    loginErrorMessage?: string,
    confirmRecoveryEmailTitle?: string,
    codeLabel?: string,
    codePlaceholder?: string,
    verifyCodeButtonLabel?: string,
    twoFactorTitle?: string,
    twoFactorDescription?: string,
    twoFactorEmailDescription?: string,
    twoFactorSupportDescription?: string,
    twoFactorSqlSetupDescription?: string,
    authenticatorManagementTitle?: string,
    authenticatorManagementDescription?: string,
    sendEmailCodeLabel?: string,
    cancelTwoFactorButtonLabel?: string,
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
    codeLabel: "Authentication code",
    codePlaceholder: "1",
    verifyCodeButtonLabel: "Verify code",
    twoFactorTitle: "Two-factor authentication",
    twoFactorDescription: "Enter the 6-digit code from your authenticator app.",
    twoFactorEmailDescription: "Enter the 6-digit code sent to your email.",
    twoFactorSupportDescription: "No email or usable authenticator is available. Contact support to recover access.",
    twoFactorSqlSetupDescription: "Scan this QR code with your authenticator app, then enter the 6-digit code.",
    authenticatorManagementTitle: "Register an authenticator",
    authenticatorManagementDescription: "Add an authenticator app to keep two-factor access available.",
    sendEmailCodeLabel: "I do not have access",
    cancelTwoFactorButtonLabel: "Back",
}

export interface LoginValues { user: string, password: string, code: string, server: string, rememberme: boolean }

export function Login(props: LoginOptions) {
    const {
        client, onStatusCompleted, userLabel, userPlaceholder, passwordLabel, passwordPlaceholder,
        rememberLabel, forgotLabel, signInButtonLabel, loginErrorMessage, confirmRecoveryEmailTitle, codeLabel, codePlaceholder, verifyCodeButtonLabel,
        twoFactorTitle, twoFactorDescription, twoFactorEmailDescription, twoFactorSupportDescription, twoFactorSqlSetupDescription,
        authenticatorManagementTitle, authenticatorManagementDescription, sendEmailCodeLabel, cancelTwoFactorButtonLabel,
    } = useComponentDefaultProps('Login', LoginDefaultProps, props);

    const modal = useModal();

    const form = useForm<LoginValues>(
        {
            initialValues: {
                user: '',
                password: '',
                code: '',
                server: '',
                rememberme: false
            }
        });

    const [status, setStatus] = useState<OperationStatus<MicroMToken | TwoFactorLoginResult>>();
    const [twoFactorState, setTwoFactorState] = useState<TwoFactorLoginResult>();
    const [emailCodeStatus, setEmailCodeStatus] = useState<OperationStatus<void>>();
    const [pendingAuthenticatorManagementStatus, setPendingAuthenticatorManagementStatus] = useState<OperationStatus<MicroMToken>>();

    const handleClick = useCallback(async (values: LoginValues) => {
        setStatus({ loading: true });
        try {
            const data = await client.login(values.user, values.password, values.rememberme);
            if ('requires_two_factor' in data && data.requires_two_factor) {
                setTwoFactorState(data);
                setEmailCodeStatus(undefined);
                setStatus(undefined);
                return;
            }
            const new_status: OperationStatus<MicroMToken | TwoFactorLoginResult> = { data };
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
        catch (e) {
            const new_status = { error: toMicroMError(e) };
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
    }, [client, onStatusCompleted]);

    const handleTwoFactorClick = useCallback(async (values: LoginValues) => {
        if (!twoFactorState?.two_factor_challenge_id) return;

        setStatus({ loading: true });
        try {
            const data = await client.login2fa(twoFactorState.two_factor_challenge_id, values.code, values.rememberme, values.user);
            const new_status = { data: data };
            if (data.claims.authenticator_management_required === true) {
                setPendingAuthenticatorManagementStatus(new_status);
                setTwoFactorState(undefined);
                setStatus(undefined);
                return;
            }
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
        catch (e) {
            const new_status = { error: toMicroMError(e) };
            setStatus(new_status);
            onStatusCompleted(new_status);
        }
    }, [client, onStatusCompleted, twoFactorState]);

    const handleSendEmailCodeClick = useCallback(async () => {
        if (!twoFactorState?.two_factor_challenge_id) return;

        setEmailCodeStatus({ loading: true });
        try {
            await client.sendTwoFactorEmailCode(twoFactorState.two_factor_challenge_id);
            setTwoFactorState({ ...twoFactorState, two_factor_flow: "email_recovery" });
            setEmailCodeStatus({ data: undefined });
            form.setFieldValue('code', '');
        }
        catch (e) {
            setEmailCodeStatus({ error: toMicroMError(e) });
        }
    }, [client, form, twoFactorState]);

    const handleForgotPasswordClick = useCallback(async () => {
        await modal.open({
            modalProps: {
                title: <Group><IconMailCheck size="1.5rem" stroke="1.5" /><Text fw="700">{confirmRecoveryEmailTitle}</Text></Group>,
            },
            content: <RecoverPasswordEmail client={client} />
        });

    }, [client, confirmRecoveryEmailTitle, modal]);

    const handleCancelTwoFactorClick = useCallback(() => {
        setTwoFactorState(undefined);
        setEmailCodeStatus(undefined);
        setStatus(undefined);
        form.setFieldValue('code', '');
    }, [form]);

    const getTwoFactorDescription = () => {
        switch (twoFactorState?.two_factor_flow) {
            case "email_setup":
            case "email_recovery":
                return twoFactorEmailDescription;
            case "support_required":
                return twoFactorSupportDescription;
            case "sql_admin_setup":
                return twoFactorSqlSetupDescription;
            default:
                return twoFactorDescription;
        }
    };

    const canEnterTwoFactorCode = twoFactorState?.two_factor_flow !== "support_required";

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
                {pendingAuthenticatorManagementStatus ? (
                    <Stack mt="md">
                        <Stack spacing={2}>
                            <Text weight={700}>{authenticatorManagementTitle}</Text>
                            <Text size="sm" color="dimmed">{authenticatorManagementDescription}</Text>
                        </Stack>
                        <TotpAuthenticatorsManagement client={client} onChanged={() => onStatusCompleted(pendingAuthenticatorManagementStatus)} />
                    </Stack>
                ) : twoFactorState ? (
                    <Stack mt="md" align="center">
                        <Stack spacing={2}>
                            <Text weight={700}>{twoFactorTitle}</Text>
                            <Text size="sm" color="dimmed">{getTwoFactorDescription()}</Text>
                        </Stack>
                        {twoFactorState.qr_code_data_url &&
                            <Image src={twoFactorState.qr_code_data_url} alt={twoFactorTitle} width={180} height={180} fit="contain" mx="auto" />
                        }
                        {twoFactorState.two_factor_flow === "authenticator" &&
                            <Anchor component="button" type="button" size="sm" disabled={status?.loading || emailCodeStatus?.loading} onClick={() => void handleSendEmailCodeClick()}>
                                {sendEmailCodeLabel}
                            </Anchor>
                        }
                        {canEnterTwoFactorCode &&
                            <>
                                <PinInput length={6} oneTimeCode type="number" aria-label={codeLabel} placeholder={codePlaceholder} disabled={status?.loading} {...form.getInputProps('code')} />
                                <Button key="login2fa" type="button" fullWidth disabled={status?.loading || form.values.code.length !== 6} onClick={() => void handleTwoFactorClick(form.values)}>
                                    {verifyCodeButtonLabel}
                                </Button>
                            </>
                        }
                        <Button key="login2fa-cancel" type="button" variant="subtle" fullWidth disabled={status?.loading} onClick={handleCancelTwoFactorClick}>
                            {cancelTwoFactorButtonLabel}
                        </Button>
                    </Stack>
                ) : (
                    <>
                        <TextInput label={userLabel} placeholder={userPlaceholder} required data-autofocus disabled={status?.loading || !!twoFactorState} {...form.getInputProps('user')} />
                        <PasswordInput label={passwordLabel} placeholder={passwordPlaceholder} required mt="md" disabled={status?.loading || !!twoFactorState}  {...form.getInputProps('password')} />
                        <Group position="apart" mt="lg">
                            <Checkbox label={rememberLabel} {...form.getInputProps('rememberme', { type: 'checkbox' })} />
                            <Anchor component="button" size="sm" onClick={handleForgotPasswordClick}>
                                {forgotLabel}
                            </Anchor>
                        </Group>
                        <Button key="login" fullWidth mt="xl" disabled={status?.loading} type="submit">
                            {signInButtonLabel}
                        </Button>
                    </>
                )}
            </form>
            <AlertError mt="xs" hidden={status?.error === undefined}>{
                (status?.error !== undefined && status.error.name === 'validation') ? status.error.message : loginErrorMessage
            }</AlertError>
            <AlertError mt="xs" hidden={emailCodeStatus?.error === undefined}>{
                emailCodeStatus?.error?.errorBody || emailCodeStatus?.error?.message || loginErrorMessage
            }</AlertError>
        </>
    );
}
