import { useState } from "react";
import { ValuesObject } from "../../client";
import { DataViewLimit } from "../DataView";

export function useViewState(search?: string[], limit?: DataViewLimit) {

    const [searchText, setSearchText] = useState<string[] | undefined>(search);
    const [limitRows, setLimitRows] = useState<string | null>(limit ? limit as string : null);
    const [refresh, setRefresh] = useState(false);

    const [filterValues, setFilterValues] = useState<ValuesObject | undefined>();
    const [filtersDescription, setFiltersDescription] = useState<ValuesObject | undefined>();

    return {
        searchText,
        setSearchText,
        limitRows,
        setLimitRows,
        refresh,
        setRefresh,
        filterValues,
        setFilterValues,
        filtersDescription,
        setFiltersDescription
    }
}