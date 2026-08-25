import { useSessionStorage } from "@mantine/hooks";
import { useCallback } from "react";

const RETAINED_SEARCH_VERSION = 1 as const;
const RETAINED_SEARCH_PREFIX = "retained-search";
const DISABLED_SEARCH_KEY = `${RETAINED_SEARCH_PREFIX}:disabled`;

export type SearchCallback = (search: string[] | undefined) => void;

interface RetainedSearchState {
    version: typeof RETAINED_SEARCH_VERSION;
    executed: boolean;
    terms: string[];
}

interface UseRetainedSearchOptions {
    retainSearch?: string;
    search?: string[];
    refreshOnInit?: boolean;
    onSearch?: SearchCallback;
    onSearchTextChange?: SearchCallback;
}

interface UseRetainedSearchResult {
    search?: string[];
    refreshOnInit?: boolean;
    onSearch?: SearchCallback;
    onSearchTextChange?: SearchCallback;
}

function createInitialState(initialSearch?: string[]): RetainedSearchState {
    return {
        version: RETAINED_SEARCH_VERSION,
        executed: false,
        terms: initialSearch ?? []
    };
}

function deserializeRetainedSearch(value: string, initialSearch?: string[]): RetainedSearchState {
    try {
        const parsed = JSON.parse(value) as Partial<RetainedSearchState>;
        if (
            parsed.version !== RETAINED_SEARCH_VERSION ||
            typeof parsed.executed !== "boolean" ||
            !Array.isArray(parsed.terms) ||
            !parsed.terms.every(term => typeof term === "string")
        ) {
            return createInitialState(initialSearch);
        }

        return parsed as RetainedSearchState;
    } catch {
        return createInitialState(initialSearch);
    }
}

export function useRetainedSearch({ retainSearch, search, refreshOnInit, onSearch, onSearchTextChange }: UseRetainedSearchOptions): UseRetainedSearchResult {
    const [state, setState] = useSessionStorage<RetainedSearchState>({
        key: retainSearch ? `${RETAINED_SEARCH_PREFIX}:${retainSearch}` : DISABLED_SEARCH_KEY,
        defaultValue: createInitialState(search),
        getInitialValueInEffect: false,
        deserialize: value => deserializeRetainedSearch(value, search)
    });

    const handleSearch: SearchCallback = useCallback(searchText => {
        setState(searchText?.length
            ? {
                version: RETAINED_SEARCH_VERSION,
                executed: true,
                terms: searchText
            }
            : createInitialState()
        );
        onSearch?.(searchText);
    }, [onSearch, setState]);

    const handleSearchTextChange: SearchCallback = useCallback(searchText => {
        if (!searchText?.length) {
            setState(createInitialState());
        }
        onSearchTextChange?.(searchText);
    }, [onSearchTextChange, setState]);

    if (!retainSearch) {
        return { search, refreshOnInit, onSearch, onSearchTextChange };
    }

    return {
        search: state.terms,
        refreshOnInit: state.executed ? true : refreshOnInit,
        onSearch: handleSearch,
        onSearchTextChange: handleSearchTextChange
    };
}
