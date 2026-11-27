import { useComponentDefaultProps } from "@mantine/core";
import { SpotlightAction, SpotlightProvider, SpotlightProviderProps } from "@mantine/spotlight";
import { ReactNode, useCallback, useMemo, useRef, useState } from "react";
import { caseInsensitveAccentInsensitiveFilter } from "../Menu/caseInsensitveAccentInsensitiveFilter";
import { GroupedSpotlightFooter, GroupedSpotlightFooterProps } from "./GroupedSpotlightFooter";
import { GroupedSpotlightGroup, GroupedSpotlightGroups, GroupedSpotlightGroupsProps } from "./GroupedSpotlightGroups";
import { GroupedSpotlightNothingFound, GroupedSpotlightNothingFoundProps } from "./GroupedSpotlightNothingFound";
import { GroupedSpotlightScrollArea } from "./GroupedSpotlightScrollArea";

export interface GroupedSpotlightProps extends Omit<SpotlightProviderProps, 'actionsWrapperComponent'> {
    showGroups?: boolean,
    showFooter?: boolean,
    resetGroupOnClose?: boolean,
    groupsProps?: Partial<Omit<GroupedSpotlightGroupsProps, 'groups' | 'total' | 'activeGroup' | 'onGroupChange'>>,
    footerProps?: GroupedSpotlightFooterProps,
    nothingFoundProps?: Omit<GroupedSpotlightNothingFoundProps, 'query' | 'activeGroup'>
}

export const GroupedSpotlightDefaultProps: Partial<GroupedSpotlightProps> = {
    showGroups: true,
    showFooter: true,
    resetGroupOnClose: true,
    filter: caseInsensitveAccentInsensitiveFilter,
    limit: Number.MAX_SAFE_INTEGER,
    scrollAreaComponent: GroupedSpotlightScrollArea
};

const filterByGroup = (actions: SpotlightAction[], group: string | null) =>
    group === null ? actions : actions.filter(action => action.group === group);


export function GroupedSpotlight(props: GroupedSpotlightProps) {
    const {
        actions, filter, query: queryProp, onQueryChange, onSpotlightClose, nothingFoundMessage,
        showGroups, showFooter, resetGroupOnClose, groupsProps, footerProps, nothingFoundProps, children, ...others
    } = useComponentDefaultProps('GroupedSpotlight', GroupedSpotlightDefaultProps, props);

    const [internalQuery, setInternalQuery] = useState('');
    const query = queryProp ?? internalQuery;
    const [activeGroup, setActiveGroup] = useState<string | null>(null);

    const handleQueryChange = useCallback((value: string) => {
        setInternalQuery(value);
        onQueryChange?.(value);
    }, [onQueryChange]);

    const handleClose = useCallback(() => {
        if (resetGroupOnClose) setActiveGroup(null);
        onSpotlightClose?.();
    }, [resetGroupOnClose, onSpotlightClose]);

    // Group names in order of first appearance: the pills do not move while the user types
    const groupNames = useMemo(() => {
        const names = new Set<string>();
        for (const action of actions) if (action.group) names.add(action.group);
        return [...names];
    }, [actions]);

    const matches = useMemo(() => filter!(query, actions), [filter, query, actions]);

    const groups = useMemo<GroupedSpotlightGroup[]>(() => {
        const counts = new Map<string, number>();
        for (const action of matches) if (action.group) counts.set(action.group, (counts.get(action.group) ?? 0) + 1);
        return groupNames.map(name => ({ name, count: counts.get(name) ?? 0 }));
    }, [matches, groupNames]);

    const visible = useMemo(() => filterByGroup(matches, activeGroup), [matches, activeGroup]);

    // Spotlight filters on every render: it gets the list already computed here, so the actions are filtered once
    const groupFilter = useCallback(
        (spotlightQuery: string, spotlightActions: SpotlightAction[]) =>
            spotlightQuery === query ? visible : filterByGroup(filter!(spotlightQuery, spotlightActions), activeGroup),
        [query, visible, filter, activeGroup]
    );

    // Spotlight renders the actions wrapper and only passes children to it. The wrapper is created once (a new
    // component on every render would remount the actions list) and reads the current data from this ref. The ref is
    // written in the same render that re-renders Spotlight (query and filter change), so the wrapper never sees stale data.
    const wrapperData = useRef({ groups, total: matches.length, activeGroup, showGroups, showFooter, groupsProps, footerProps });
    wrapperData.current = { groups, total: matches.length, activeGroup, showGroups, showFooter, groupsProps, footerProps };

    const ActionsWrapper = useMemo(() => function GroupedSpotlightActions({ children: actionsList }: { children: ReactNode }) {
        const data = wrapperData.current;
        return (
            <>
                {data.showGroups &&
                    <GroupedSpotlightGroups
                        {...data.groupsProps}
                        groups={data.groups}
                        total={data.total}
                        activeGroup={data.activeGroup}
                        onGroupChange={setActiveGroup}
                    />
                }
                {actionsList}
                {data.showFooter && <GroupedSpotlightFooter {...data.footerProps} />}
            </>
        );
    }, []);

    return (
        <SpotlightProvider
            {...others}
            actions={actions}
            filter={groupFilter}
            query={query}
            onQueryChange={handleQueryChange}
            onSpotlightClose={handleClose}
            actionsWrapperComponent={ActionsWrapper}
            nothingFoundMessage={nothingFoundMessage ?? <GroupedSpotlightNothingFound {...nothingFoundProps} query={query} activeGroup={activeGroup} />}
        >
            {children}
        </SpotlightProvider>
    );
}
