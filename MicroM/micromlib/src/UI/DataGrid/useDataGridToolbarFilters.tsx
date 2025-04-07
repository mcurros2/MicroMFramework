import { MantineNumberSize, SelectItem } from "@mantine/core";
import { Dispatch, SetStateAction, useCallback, useEffect, useState } from "react";
import { ColumnsObject, EntityConstructor, SYSTEM_COLUMNS_NAMES, setValues } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { useOpenForm } from "../Core";

export interface UseDataGridToolbarFiltersProps {
    filterValues: ValuesObject | undefined,
    setFilterValues: Dispatch<SetStateAction<ValuesObject | undefined>>,

    filtersDescription: ValuesObject | undefined,
    setFiltersDescription: Dispatch<SetStateAction<ValuesObject | undefined>>,

    client?: MicroMClient,
    parentKeys?: ValuesObject,
    FiltersEntity?: EntityConstructor,
    searchData: SelectItem[],
    setSearchData: Dispatch<SetStateAction<SelectItem[]>>,
    setSearchText: Dispatch<SetStateAction<string[] | undefined>>,
    onRefreshClick: (searchText: string[] | undefined) => void,
    onSearchTextChange?: (text: string[] | undefined) => void,
    filtersFormSize: MantineNumberSize,
    visibleFilters?: string[],

    initialColumnFilters?: ColumnsObject
}

export function useDataGridToolbarFilters(props: UseDataGridToolbarFiltersProps) {
    const {
        filterValues, setFilterValues, client,
        parentKeys, FiltersEntity, setSearchData, setSearchText, onRefreshClick, onSearchTextChange,
        searchData, filtersFormSize, visibleFilters, filtersDescription, setFiltersDescription, initialColumnFilters
    } = props;

    const openFilters = useOpenForm();

    const updateFilterValuesAndDescription = useCallback((columns: ColumnsObject) => {
        const values: ValuesObject = {};
        const filter_description: ValuesObject = {};
        for (const col_name in columns) {
            if (!SYSTEM_COLUMNS_NAMES.includes(col_name)) {
                const value = columns[col_name].value;
                const col = columns[col_name];
                values[col_name] = value;
                if (visibleFilters === undefined || (visibleFilters && visibleFilters.includes(col_name))) {
                    if (value && (value !== col.defaultValue)) {
                        filter_description[col.prompt] = columns[col_name].valueDescription || value;
                    }
                    else {
                        delete filter_description[col.prompt];
                    }
                }
            }
        }
        setFilterValues(values);
        setFiltersDescription(filter_description);
    }, [setFilterValues, setFiltersDescription, visibleFilters]);

    const handleFilterButtonClick = useCallback(async (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (!client || !FiltersEntity) return;

        const filter_entity = FiltersEntity(client, parentKeys);
        setValues(filter_entity.def.columns, filterValues);
        Object.values(filter_entity.def.columns).forEach(col => {
            if (filtersDescription && filtersDescription[col.prompt]) {
                const description = filtersDescription[col.prompt];
                if (description && typeof description === 'string') {
                    col.valueDescription = description;
                }
            }
        });

        if (!filter_entity.Form) {
            console.error(`FiltersEntity ${FiltersEntity.name} does not have a Form component`);
            return;
        }

        await openFilters({
            entity: filter_entity,
            initialFormMode: 'add',
            OKText: 'Apply',
            modalFormSize: filtersFormSize,
            onModalSaved: async () => {
                updateFilterValuesAndDescription(filter_entity.def.columns);
            }
        });

    }, [FiltersEntity, client, filterValues, filtersDescription, filtersFormSize, openFilters, parentKeys, updateFilterValuesAndDescription]);

    // Search
    const [queryText, setQueryText] = useState<string>('');

    const createSearchPhrase = useCallback((filter: string) => {
        const item = { value: (filter.indexOf('%') >= 0 ? filter.toLowerCase() : `%${filter.toLowerCase()}%`), label: filter };
        setSearchData((current) => {
            const isDuplicate = current.some(x => x.value === item.value);
            return isDuplicate ? current : [...current, item];
        });
        return item;
    }, [setSearchData]);

    const handleSearchFilterInputEnter = useCallback((e: React.KeyboardEvent) => {
        const key_code = e.code;
        if (key_code !== "Enter" && key_code !== "NumpadEnter") return;

        // Ensure queryText is not empty
        if (!queryText.trim()) return;

        // Create the search item and add to searchData if not already included
        const newItem = createSearchPhrase(queryText); // This will add only if it's not a duplicate

        // Add queryText to searchText array if not already selected
        setSearchText(prevText => {
            const updatedText = prevText!.includes(newItem.value) ? prevText : [...prevText!, newItem.value];

            // Trigger onRefreshClick with the updated searchText array
            if (onRefreshClick) {
                onRefreshClick(updatedText);
            }

            return updatedText;
        });

        // Clear the queryText
        setQueryText('');

    }, [createSearchPhrase, onRefreshClick, queryText, setSearchText]);


    const handleSearchFilterInputOnChange = useCallback((searchFilter: string[]) => {

        setSearchText(searchFilter);

        // FIX: when creating an item clicking create filter in the list, the searchData state is not upated yet and doesn't include the new item
        // we need to update searchData only when an item is deleted from the searchFilter
        if (searchFilter.length < searchData.length) {
            const updatedSearchData = searchData.filter(item => searchFilter.includes(item.value));
            setSearchData(updatedSearchData);
        }

        if (onSearchTextChange) onSearchTextChange(searchFilter);

    }, [onSearchTextChange, searchData, setSearchData, setSearchText]);


    const handleClearFiltersClick = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (!client || !FiltersEntity) return;
        const filter_entity = FiltersEntity(client, parentKeys);
        updateFilterValuesAndDescription(filter_entity.def.columns);
    }, [FiltersEntity, client, parentKeys, updateFilterValuesAndDescription]);

    useEffect(() => {
        if (!client || !FiltersEntity) return;
        const filter_entity = FiltersEntity(client, parentKeys);
        setValues(filter_entity.def.columns, initialColumnFilters, null, true);
        updateFilterValuesAndDescription(filter_entity.def.columns);
    }, []);

    return {
        handleFilterButtonClick,
        handleSearchFilterInputEnter,
        handleClearFiltersClick,
        createSearchPhrase,
        queryText,
        filtersDescription,
        setFiltersDescription,
        setQueryText,
        handleSearchFilterInputOnChange,
        updateFilterValuesAndDescription
    }
}