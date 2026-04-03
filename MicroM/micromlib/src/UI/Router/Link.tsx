import { Anchor, AnchorProps } from "@mantine/core";
import { forwardRef } from "react";
import { useMicroMRouter } from "./useMicroMRouter";

export interface LinkProps extends Omit<AnchorProps, 'href'> {
    to: string;
    children: React.ReactNode;
}

export const Link = forwardRef<HTMLAnchorElement, LinkProps>(function Link(props: LinkProps, ref) {
    const { navigate } = useMicroMRouter();
    const { to, children } = props;

    return <Anchor {...props} ref={ref} href={to} onClick={(e) => { e.preventDefault(); navigate(to); } }>{children}</Anchor>;
});
