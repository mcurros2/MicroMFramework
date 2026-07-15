import { Button, Image, PinInput, Stack, Text, TextInput, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useState } from "react";
import { MicroMClient, OperationStatus, toMicroMError, TotpSetupStartResponse } from "../../client";
import { AlertError, AlertSuccess, FakeProgressBar } from "../Core";

export interface TotpSetupProps {
    client: MicroMClient,
    onConfirmed?: () => void,
    setupButtonLabel?: string,
    confirmButtonLabel?: string,
    qrAlt?: string,
    codePlaceholder?: string,
    setupTitle?: string,
    setupDescription?: string,
    authenticatorNameLabel?: string,
    authenticatorNamePlaceholder?: string,
    successMessage?: string,
    errorMessage?: string,
}

export const TotpSetupDefaultProps: Partial<TotpSetupProps> = {
    setupButtonLabel: "Set up authenticator app",
    confirmButtonLabel: "Confirm code",
    qrAlt: "Authenticator setup QR code",
    codePlaceholder: "123456",
    setupTitle: "Authenticator app",
    setupDescription: "Scan this QR code with your authenticator app, then enter the 6-digit code.",
    authenticatorNameLabel: "Authenticator name",
    authenticatorNamePlaceholder: "My phone",
    successMessage: "Authenticator app is enabled.",
    errorMessage: "The authenticator request could not be completed.",
}

export function TotpSetup(props: TotpSetupProps) {
    const {
        client, onConfirmed, setupButtonLabel, confirmButtonLabel, qrAlt, codePlaceholder,
        setupTitle, setupDescription, authenticatorNameLabel, authenticatorNamePlaceholder, successMessage, errorMessage
    } = useComponentDefaultProps('TotpSetup', TotpSetupDefaultProps, props);

    const [setupResponse, setSetupResponse] = useState<TotpSetupStartResponse>();
    const [authenticatorName, setAuthenticatorName] = useState("");
    const [code, setCode] = useState("");
    const [status, setStatus] = useState<OperationStatus<TotpSetupStartResponse | null>>();
    const [confirmed, setConfirmed] = useState(false);

    const startSetup = useCallback(async () => {
        if (!authenticatorName.trim()) return;

        setStatus({ loading: true });
        setConfirmed(false);
        try {
            const data = await client.startTotpSetup(authenticatorName.trim());
            setSetupResponse(data);
            setCode("");
            setStatus({ data });
        }
        catch (e) {
            setStatus({ error: toMicroMError(e) });
        }
    }, [authenticatorName, client]);

    const confirmSetup = useCallback(async () => {
        if (code.length !== 6 || !setupResponse) return;

        setStatus({ loading: true });
        try {
            await client.confirmTotpSetup(code, setupResponse.setup_challenge_id);
            setConfirmed(true);
            setSetupResponse(undefined);
            setAuthenticatorName("");
            setCode("");
            setStatus({ data: null });
            onConfirmed?.();
        }
        catch (e) {
            setStatus({ error: toMicroMError(e) });
        }
    }, [client, code, onConfirmed, setupResponse]);

    return (
        <Stack>
            {status?.loading && <FakeProgressBar />}
            <Stack spacing={4}>
                <Text weight={700}>{setupTitle}</Text>
                <Text size="sm" color="dimmed">{setupDescription}</Text>
            </Stack>
            {!setupResponse &&
                <>
                    <TextInput
                        label={authenticatorNameLabel}
                        placeholder={authenticatorNamePlaceholder}
                        value={authenticatorName}
                        disabled={status?.loading}
                        onChange={(event) => setAuthenticatorName(event.currentTarget.value)}
                    />
                    <Button type="button" disabled={status?.loading || !authenticatorName.trim()} onClick={() => void startSetup()}>
                        {setupButtonLabel}
                    </Button>
                </>
            }
            {setupResponse &&
                <Stack>
                    <Image src={setupResponse.qr_code_data_url} alt={qrAlt} width={220} fit="contain" />
                    <PinInput
                        length={6}
                        oneTimeCode
                        type="number"
                        placeholder={codePlaceholder}
                        disabled={status?.loading}
                        value={code}
                        onChange={setCode}
                    />
                    <Button type="button" disabled={status?.loading || code.length !== 6} onClick={() => void confirmSetup()}>
                        {confirmButtonLabel}
                    </Button>
                </Stack>
            }
            <AlertSuccess hidden={!confirmed}>{successMessage}</AlertSuccess>
            <AlertError hidden={status?.error === undefined}>
                {status?.error?.errorBody || status?.error?.message || errorMessage}
            </AlertError>
        </Stack>
    );
}
