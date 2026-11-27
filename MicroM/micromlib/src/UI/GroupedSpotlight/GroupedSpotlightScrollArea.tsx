import { ScrollArea, ScrollAreaAutosizeProps, useComponentDefaultProps } from "@mantine/core";
import { ComponentType } from "react";

export type GroupedSpotlightScrollAreaProps = ScrollAreaAutosizeProps;

export const GroupedSpotlightScrollAreaDefaultProps: Partial<GroupedSpotlightScrollAreaProps> = {
    // Leaves room for the search input, the group pills and the footer inside the modal
    mah: 'calc(100vh - 24rem)'
};

// Mantine 6 types ScrollArea.Autosize as Pick<ScrollAreaAutosizeProps, ...> built against older React typings, so with
// the installed @types/react it asks for placeholder, onPointerEnterCapture and onPointerLeaveCapture (TS2739).
// Types only: the component accepts ScrollAreaAutosizeProps at runtime.
const ScrollAreaAutosize = ScrollArea.Autosize as unknown as ComponentType<ScrollAreaAutosizeProps>;

export function GroupedSpotlightScrollArea(props: GroupedSpotlightScrollAreaProps) {
    const {
        children, ...others
    } = useComponentDefaultProps('GroupedSpotlightScrollArea', GroupedSpotlightScrollAreaDefaultProps, props);

    return (
        <ScrollAreaAutosize {...others}>
            {children}
        </ScrollAreaAutosize>
    );
}
