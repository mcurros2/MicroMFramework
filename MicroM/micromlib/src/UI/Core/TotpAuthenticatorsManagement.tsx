import { ActionIcon, Group, Stack, Text, Tooltip, useComponentDefaultProps } from "@mantine/core";
import { IconTrash } from "@tabler/icons-react";
import { useCallback, useEffect, useState } from "react";
import { MicroMClient, OperationStatus, toMicroMError, TotpAuthenticatorsResponse } from "../../client";
import { AlertError } from "./AlertError";
import { FakeProgressBar } from "./FakeProgressBar";
import { TotpSetup } from "./TotpSetup";

export interface TotpAuthenticatorsManagementProps {
    client: MicroMClient,
    title?: string,
    emptyMessage?: string,
    deleteLabel?: string,
    errorMessage?: string,
    onChanged?: () => void,
}

export const TotpAuthenticatorsManagementDefaultProps: Partial<TotpAuthenticatorsManagementProps> = {
    title: "Registered authenticators",
    emptyMessage: "No authenticators are registered.",
    deleteLabel: "Delete authenticator",
    errorMessage: "The authenticator request could not be completed.",
}

export function TotpAuthenticatorsManagement(props: TotpAuthenticatorsManagementProps) {
    const { client, title, emptyMessage, deleteLabel, errorMessage, onChanged } = useComponentDefaultProps('TotpAuthenticatorsManagement', TotpAuthenticatorsManagementDefaultProps, props);
    const [status, setStatus] = useState<OperationStatus<TotpAuthenticatorsResponse>>();

    const loadAuthenticators = useCallback(async () => {
        setStatus({ loading: true });
        try {
            const data = await client.listTotpAuthenticators();
            setStatus({ data });
        }
        catch (e) {
            setStatus({ error: toMicroMError(e) });
        }
    }, [client]);

    const deleteAuthenticator = useCallback(async (authenticatorId: string) => {
        setStatus({ loading: true });
        try {
            await client.deleteTotpAuthenticator(authenticatorId);
            await loadAuthenticators();
            onChanged?.();
        }
        catch (e) {
            setStatus({ error: toMicroMError(e) });
        }
    }, [client, loadAuthenticators, onChanged]);

    useEffect(() => {
        loadAuthenticators();
    }, [loadAuthenticators]);

    return (
        <Stack>
            {status?.loading && <FakeProgressBar />}
            <Stack spacing={6}>
                <Text weight={700}>{title}</Text>
                {status?.data?.authenticators.length === 0 && <Text size="sm" color="dimmed">{emptyMessage}</Text>}
                {status?.data?.authenticators.map((authenticator) => (
                    <Group key={authenticator.authenticator_id} position="apart" noWrap>
                        <Text size="sm">{authenticator.authenticator_name}</Text>
                        <Tooltip label={deleteLabel}>
                            <ActionIcon
                                color="red"
                                variant="subtle"
                                disabled={status?.loading}
                                onClick={() => void deleteAuthenticator(authenticator.authenticator_id)}
                            >
                                <IconTrash size="1rem" />
                            </ActionIcon>
                        </Tooltip>
                    </Group>
                ))}
            </Stack>
            <TotpSetup client={client} onConfirmed={() => { void loadAuthenticators(); onChanged?.(); }} />
            <AlertError hidden={status?.error === undefined}>
                {status?.error?.errorBody || status?.error?.message || errorMessage}
            </AlertError>
        </Stack>
    );
}
