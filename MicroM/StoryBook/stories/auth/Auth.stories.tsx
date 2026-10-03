import { Button, Center, Code, Group, Paper, Stack, Text } from "@mantine/core";
import {
    Login as MicroMLogin, LoginModal as MicroMLoginModal, LoginModalForm as MicroMLoginModalForm, RecoverPassword as MicroMRecoverPassword, RecoverPasswordEmail as MicroMRecoverPasswordEmail, TotpAuthenticatorsManagement as MicroMTotpAuthenticatorsManagement, TotpSetup as MicroMTotpSetup, useLogin
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { ReactNode, useMemo, useState } from "react";
import { MockTwoFactorFlow } from "../mocks/MockMicroMClient";
import { createMockClient } from "../mocks/mockClient";

function Frame({ children, note }: { children: ReactNode, note?: ReactNode }) {
    return (
        <Center mih="80vh">
            <Stack w="26rem">
                <Paper withBorder shadow="md" p="xl">{children}</Paper>
                {note && <Text size="xs" color="dimmed">{note}</Text>}
            </Stack>
        </Center>
    );
}

function LoginStory({ flow }: { flow?: MockTwoFactorFlow }) {
    const client = useMemo(() => createMockClient({ latency: 600, loggedIn: false, twoFactorFlow: flow }), [flow]);
    const [status, setStatus] = useState("");
    return (
        <Frame note={flow ? <>2FA flow <Code>{flow}</Code>: any user/password, then code <Code>123456</Code>.</> : "Any user/password works (mock)."}>
            <MicroMLogin client={client} onStatusCompleted={s => setStatus(s.error ? `Error: ${s.error.message ?? s.error.statusMessage}` : "Logged in")} />
            {status && <Text size="sm" mt="md" color="dimmed">{status}</Text>}
        </Frame>
    );
}

const meta = {
    title: "MicroM/Auth",
    parameters: { layout: "fullscreen", controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const Login: Story = { name: "Login", render: () => <LoginStory /> };
export const LoginAuthenticator: Story = { name: "Login - 2FA authenticator", render: () => <LoginStory flow="authenticator" /> };
export const LoginEmailSetup: Story = { name: "Login - 2FA email setup", render: () => <LoginStory flow="email_setup" /> };
export const LoginSqlAdminSetup: Story = { name: "Login - 2FA admin setup (QR)", render: () => <LoginStory flow="sql_admin_setup" /> };
export const LoginSupportRequired: Story = { name: "Login - 2FA support required", render: () => <LoginStory flow="support_required" /> };

function LoginModalStory() {
    const client = useMemo(() => createMockClient({ loggedIn: false }), []);
    const [claims, setClaims] = useState<unknown>();
    const [show, setShow] = useState(true);
    return (
        <Center mih="60vh">
            <Stack align="center">
                <Group><Button onClick={() => setShow(true)}>Open LoginModal</Button></Group>
                <Text size="sm">onLoggedIn claims: <Code>{JSON.stringify(claims ?? null)}</Code></Text>
                {show && <MicroMLoginModal client={client} onLoggedIn={c => { setClaims(c); setShow(false); }} onClose={() => setShow(false)} />}
            </Stack>
        </Center>
    );
}

export const LoginModal: Story = { name: "LoginModal", render: () => <LoginModalStory /> };

function LoginModalFormStory() {
    const client = useMemo(() => createMockClient({ loggedIn: false }), []);
    const [open, setOpen] = useState(false);
    const [claims, setClaims] = useState<unknown>();
    return (
        <Center mih="60vh">
            <Stack align="center">
                <Button onClick={() => setOpen(true)}>Open LoginModalForm</Button>
                <Text size="sm">onLoggedIn claims: <Code>{JSON.stringify(claims ?? null)}</Code></Text>
                <MicroMLoginModalForm client={client} openState={open} onLoggedIn={c => { setClaims(c); setOpen(false); }} onClose={() => setOpen(false)} />
            </Stack>
        </Center>
    );
}

export const LoginModalForm: Story = { name: "LoginModalForm", render: () => <LoginModalFormStory /> };

function UseLoginStory() {
    const client = useMemo(() => createMockClient({ loggedIn: false }), []);
    const login = useLogin({ client, title: "Sign-in" });
    return <Center mih="60vh"><Button onClick={() => login()}>useLogin()()</Button></Center>;
}

export const UseLogin: Story = { name: "useLogin", render: () => <UseLoginStory /> };

export const RecoverPasswordEmail: Story = {
    name: "RecoverPasswordEmail",
    render: () => <Frame><MicroMRecoverPasswordEmail client={createMockClient()} /></Frame>,
};

export const RecoverPassword: Story = {
    name: "RecoverPassword",
    render: () => <Frame note="Reads ?code= from the URL in a real app; type any code here."><MicroMRecoverPassword client={createMockClient()} /></Frame>,
};

export const TotpSetup: Story = {
    name: "TotpSetup",
    render: () => <Frame note="Confirm with code 123456."><MicroMTotpSetup client={createMockClient()} /></Frame>,
};

export const TotpAuthenticatorsManagement: Story = {
    name: "TotpAuthenticatorsManagement",
    render: () => <Frame note="Lists, deletes and adds authenticators (add confirms with 123456)."><MicroMTotpAuthenticatorsManagement client={createMockClient()} /></Frame>,
};
