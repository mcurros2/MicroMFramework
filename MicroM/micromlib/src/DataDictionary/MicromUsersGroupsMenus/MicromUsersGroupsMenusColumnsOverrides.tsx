import { Group, MantineTheme } from "@mantine/core";
import { IconCheck, IconX } from "@tabler/icons-react";
import { CircleFilledIcon } from "../../UI";

export interface MicromUsersGroupsMenusColumnsOverridesLabels {
    granted: string,
    denied: string,
}

export const MicromUsersGroupsMenusColumnsOverridesLabelsDefaultProps: Partial<MicromUsersGroupsMenusColumnsOverridesLabels> = {
    granted: "Granted",
    denied: "Denied",
}

export function MicromUsersGroupsMenusColumnsOverrides(theme: MantineTheme) {
    const l = MicromUsersGroupsMenusColumnsOverridesLabelsDefaultProps;
    return {
        // Access
        4: {
            render: (value: unknown) => (value === "true") ? <Group style={{ gap: '0rem' }} noWrap>
                <CircleFilledIcon mr="xs" backColor={theme.colors.green[8]} width="1.3rem" icon={<IconCheck size="0.9rem" />}></CircleFilledIcon>{l.granted}
            </Group> : <Group style={{ gap: '0rem' }} noWrap>
                <CircleFilledIcon mr="xs" backColor={theme.colors.red[8]} width="1.3rem" icon={<IconX size="0.9rem" />}></CircleFilledIcon>{l.denied}
            </Group>
        },
    }

}