import { Button, PasswordInput, TextInput, useComponentDefaultProps } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useCallback, useEffect, useState } from "react";
import { DBStatusResult, MicroMClient, OperationStatus, StatusCompletedHandler, toMicroMError } from "../../client";
import { AlertError, AlertSuccess, FakeProgressBar } from "../Core";
import { getWindowLocationQueryStringAsObject } from "../Form";

export interface RecoverPasswordOptions {
    client: MicroMClient,
    changedSuccessfullyURL?: string,
    onStatusCompleted?: StatusCompletedHandler<DBStatusResult>,
    userLabel?: string,
    userPlaceholder?: string,
    passwordLabel?: string,
    passwordPlaceholder?: string,
    changePasswordLabel?: string,
    changeErrorMessage?: string,
    changedSuccessfullyMessage?: string,
    signInButtonLabel?: string,
    passwordMinLength?: number,
    passwordMaxLength?: number,
}

export const RecoverPasswordDefaultProps: Partial<RecoverPasswordOptions> = {
    userLabel: "User",
    userPlaceholder: "you@email.com",
    passwordLabel: "New Password",
    passwordPlaceholder: "Your new password",
    changePasswordLabel: "Change password",
    changeErrorMessage: "Is not possible to change your password, request a new recovery email",
    changedSuccessfullyMessage: "Your password has been changed successfully",
    changedSuccessfullyURL: "/",
    signInButtonLabel: "Sign in",
    passwordMinLength: 8,
    passwordMaxLength: 255
}

export interface RecoverPasswordValues { username: string, password: string, recovery_code: string }

export function RecoverPassword(props: RecoverPasswordOptions) {
    const {
        client, onStatusCompleted, userLabel, userPlaceholder, passwordLabel, passwordPlaceholder,
        changePasswordLabel, changeErrorMessage, changedSuccessfullyMessage, changedSuccessfullyURL,
        signInButtonLabel, passwordMaxLength, passwordMinLength
    } = useComponentDefaultProps('Login', RecoverPasswordDefaultProps, props);

    const form = useForm<RecoverPasswordValues>(
        {
            initialValues: {
                username: '',
                password: '',
                recovery_code: ''
            }
        });

    const [status, setStatus] = useState<OperationStatus<DBStatusResult>>();

    const handleClick = useCallback(async (values: RecoverPasswordValues) => {
        setStatus({ loading: true });
        try {
            const data: DBStatusResult = await client.recoverpassword(values.username, values.password, values.recovery_code);
            const new_status = { data: data };
            setStatus(new_status);
            if (onStatusCompleted) onStatusCompleted(new_status);
        }
        catch (e) {
            const new_status = { error: toMicroMError(e) };
            setStatus(new_status);
            if (onStatusCompleted) onStatusCompleted(new_status);
        }
    }, [client, onStatusCompleted]);

    useEffect(() => {
        const params = getWindowLocationQueryStringAsObject();
        if (params.code) {
            form.values['recovery_code'] = params.code;
        }
    }, []);

    return (
        <>
            <form onSubmit={form.onSubmit((values) => handleClick(values))}>
                {status?.loading && <FakeProgressBar />}
                {!(status?.data?.Results[0].Status === 0) &&
                    <>
                        <TextInput label={userLabel} placeholder={userPlaceholder} required data-autofocus disabled={status?.loading} {...form.getInputProps('username')} />
                        <PasswordInput
                            label={passwordLabel}
                            placeholder={passwordPlaceholder}
                            required mt="md"
                            disabled={status?.loading}
                            minLength={passwordMinLength}
                            maxLength={passwordMaxLength}
                            {...form.getInputProps('password')}
                        />
                        <Button fullWidth mt="xl" disabled={status?.loading || status?.data?.Failed || status?.error !== undefined} type="submit">
                            {changePasswordLabel}
                        </Button>
                    </>
                }
                {status?.data?.Results[0].Status === 0 &&
                    <>
                        <AlertSuccess>{changedSuccessfullyMessage}</AlertSuccess>
                        <Button fullWidth mt="xl" onClick={() => window.location.href = changedSuccessfullyURL!}>
                            {signInButtonLabel}
                        </Button>
                    </>
                }
            </form>
            {status?.error &&
                <AlertError mt="xs">{changeErrorMessage}</AlertError>
            }
            {status?.data?.Failed &&
                <AlertError mt="xs">{status.data.Results[0].Message}</AlertError>
            }
        </>
    );
}