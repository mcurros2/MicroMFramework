import { Anchor } from "@mantine/core";
import { useMicroMRouter } from "./useMicroMRouter";

export interface LinkProps {
    to: string;
    children: React.ReactNode;
}

export function Link(props: LinkProps) {
    const { navigate } = useMicroMRouter();
    const { to, children } = props;

    return <Anchor href={to} onClick={(e) => { e.preventDefault; navigate(to); } }>{children}</Anchor>;
};
