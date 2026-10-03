import { DocsContainer, DocsContainerProps } from "@storybook/addon-docs/blocks";
import { PropsWithChildren, useEffect, useState } from "react";
import { GLOBALS_UPDATED } from "storybook/internal/core-events";
import { themes } from "storybook/theming";

type Globals = Record<string, unknown>;

function initialScheme(context: DocsContainerProps["context"]) {
    const store = (context as unknown as { store?: { userGlobals?: { globals?: Globals } } }).store;
    return store?.userGlobals?.globals?.colorScheme === "dark" ? "dark" : "light";
}

export function ThemedDocsContainer({ context, children, ...rest }: PropsWithChildren<DocsContainerProps>) {
    const [scheme, setScheme] = useState<"light" | "dark">(() => initialScheme(context));

    useEffect(() => {
        const onGlobals = ({ globals }: { globals: Globals }) => setScheme(globals.colorScheme === "dark" ? "dark" : "light");
        context.channel.on(GLOBALS_UPDATED, onGlobals);
        return () => { context.channel.off(GLOBALS_UPDATED, onGlobals); };
    }, [context.channel]);

    return (
        <DocsContainer {...rest} context={context} theme={scheme === "dark" ? themes.dark : themes.light}>
            {children}
        </DocsContainer>
    );
}
