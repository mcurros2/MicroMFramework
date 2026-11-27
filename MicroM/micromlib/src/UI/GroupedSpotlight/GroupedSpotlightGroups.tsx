import { Group, GroupProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode } from "react";
import { GroupedSpotlightGroupPill, GroupedSpotlightGroupPillProps } from "./GroupedSpotlightGroupPill";

export interface GroupedSpotlightGroup {
    name: string,
    count: number
}

export interface GroupedSpotlightGroupsProps extends Omit<GroupProps, 'children'> {
    groups: GroupedSpotlightGroup[],
    total: number,
    activeGroup: string | null,
    onGroupChange: (group: string | null) => void,
    allLabel?: ReactNode,
    hideEmptyGroups?: boolean,
    scrollable?: boolean,
    pillProps?: Partial<Omit<GroupedSpotlightGroupPillProps, 'label' | 'count' | 'active' | 'onClick'>>
}

export const GroupedSpotlightGroupsDefaultProps: Partial<GroupedSpotlightGroupsProps> = {
    allLabel: 'All',
    hideEmptyGroups: false,
    scrollable: true,
    spacing: 'xs',
    px: 'sm',
    pt: 'sm',
    pb: 4
};

export function GroupedSpotlightGroups(props: GroupedSpotlightGroupsProps) {
    const {
        groups, total, activeGroup, onGroupChange, allLabel, hideEmptyGroups, scrollable, pillProps, sx, ...others
    } = useComponentDefaultProps('GroupedSpotlightGroups', GroupedSpotlightGroupsDefaultProps, props);

    if (groups.length === 0) return null;

    const visibleGroups = hideEmptyGroups
        ? groups.filter(group => group.count > 0 || group.name === activeGroup)
        : groups;

    return (
        <Group
            {...others}
            role="group"
            noWrap={scrollable}
            sx={[
                scrollable ? { overflowX: 'auto', flexShrink: 0, '& > *': { flexShrink: 0 } } : { flexShrink: 0 },
                ...(Array.isArray(sx) ? sx : [sx])
            ]}
        >
            <GroupedSpotlightGroupPill
                {...pillProps}
                label={allLabel}
                count={total}
                active={activeGroup === null}
                onClick={() => onGroupChange(null)}
            />
            {visibleGroups.map(group => (
                <GroupedSpotlightGroupPill
                    {...pillProps}
                    key={group.name}
                    label={group.name}
                    count={group.count}
                    active={activeGroup === group.name}
                    disabled={group.count === 0 && activeGroup !== group.name}
                    onClick={() => onGroupChange(group.name)}
                />
            ))}
        </Group>
    );
}
