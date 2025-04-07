import { Button, TextInput, useComponentDefaultProps } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useCallback, useState } from "react";
import { DBStatusResult, MicroMClient, OperationStatus, StatusCompletedHandler, toMicroMError } from "../../client";
import { AlertError, AlertSuccess, FakeProgressBar } from "../Core";

export interface RecoverPasswordEmailOptions {
    client: MicroMClient,
    onStatusCompleted?: StatusCompletedHandler<DBStatusResult>,
    userLabel?: string,
    userPlaceholder?: string,
    emailSentSuccessfullyMessage?: string,
    sendRecoveryEmailLabel?: string,
    emailSendErrorMessage?: string,
    goBackToHomePageLabel?: string
}

export const RecoverPasswordEmailDefaultProps: Partial<RecoverPasswordEmailOptions> = {
    userLabel: "User",
    userPlaceholder: "you@email.com",
    sendRecoveryEmailLabel: "Send recovery email",
    emailSentSuccessfullyMessage: "Recovery email sent successfully",
    emailSendErrorMessage: "Is not possible to send the recovery email",
    goBackToHomePageLabel: "Go back to home page"
}

export interface RecoverPasswordEmailValues { username: string }

export function RecoverPasswordEmail(props: RecoverPasswordEmailOptions) {
    const {
        client, onStatusCompleted, userLabel, userPlaceholder, emailSentSuccessfullyMessage, 
        sendRecoveryEmailLabel, emailSendErrorMessage, goBackToHomePageLabel
    } = useComponentDefaultProps('Login', RecoverPasswordEmailDefaultProps, props);

    const form = useForm<RecoverPasswordEmailValues>(
        {
            initialValues: {
                username: '',
            }
        });

    const [status, setStatus] = useState<OperationStatus<DBStatusResult>>();

    const handleClick = useCallback(async (values: RecoverPasswordEmailValues) => {
        setStatus({ loading: true });
        try {
            const data: DBStatusResult = await client.recoveryemail(values.username);
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

    return (
        <>
            <form onSubmit={form.onSubmit((values) => handleClick(values))}>
                {status?.loading && <FakeProgressBar />}
                {!(status?.data?.Results[0].Status === 0) &&
                    <>
                        <TextInput label={userLabel} placeholder={userPlaceholder} required data-autofocus disabled={status?.loading} {...form.getInputProps('username')} />
                        <Button fullWidth mt="xl" disabled={status?.loading || status?.data?.Failed || status?.error !== undefined} type="submit">
                            {sendRecoveryEmailLabel}
                        </Button>
                    </>
                }
                {status?.data?.Results[0].Status === 0 &&
                    <>
                        <AlertSuccess>{emailSentSuccessfullyMessage}</AlertSuccess>
                        <Button fullWidth mt="xl" onClick={() => window.location.href = '/'}>
                            {goBackToHomePageLabel}
                        </Button>
                    </>
                }
            </form>
            {status?.error &&
                <AlertError mt="xs">{emailSendErrorMessage}</AlertError>
            }
            {status?.data?.Failed &&
                <AlertError mt="xs">{status.data.Results[0].Message}</AlertError>
            }
        </>
    );
}