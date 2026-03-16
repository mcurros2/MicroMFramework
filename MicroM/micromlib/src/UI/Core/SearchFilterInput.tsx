import { ActionIcon, ComboboxData, MultiSelect, MultiSelectProps, useDirection, useProps, useMantineTheme } from '@mantine/core';
import { IconArrowLeft, IconArrowRight, IconSearch } from '@tabler/icons-react';
import { forwardRef } from 'react';
import { ActionIconVariant } from '../Core';

export interface SearchFilterInputProps extends MultiSelectProps {
    onSearchClick: React.MouseEventHandler,
    size: string,
    iconsSize: string,
    placeholder?: string,
    autoFocus?: boolean,
    addFilterLabel?: string,
    creatable?: boolean,
    onCreate?: (value: string) => { value: string; label: string } | string,
    iconVariant?: ActionIconVariant,
    [x: string]: any
}


export const SearchFilterInputDefaultProps: Partial<SearchFilterInputProps> = {
    iconVariant: "light",
    placeholder: "Enter text to search",
    addFilterLabel: "+ Filter:"
}

export const SearchFilterInput = forwardRef<HTMLInputElement, SearchFilterInputProps>(function SearchFilterInput(props: SearchFilterInputProps, ref) {
    const {
        onSearchClick, size, iconsSize, value, onChange, placeholder, autoFocus, iconVariant, addFilterLabel,
        onSearchChange, searchValue, data, onOptionSubmit, creatable, onCreate,
        ...rest
    } = useProps('SearchFilterInput', SearchFilterInputDefaultProps, props);

    const theme = useMantineTheme();
    const { dir } = useDirection();
    const isRTL = dir === 'rtl';

    // Mantine has a bug in thsi component:
    // When all the items are selected and the multiselect is in a modal and it has autofocus
    // you will need to press escape two time to close the modal
    // this is because it uses a popover with closeOnEscape and it traps escape even if the list is not shown

    const baseData = (data ?? []) as ComboboxData;
    const baseValues = new Set(
        baseData.flatMap((item) => {
            if (typeof item === 'string') return [item];
            if ('group' in item) {
                return item.items.map((groupItem) => typeof groupItem === 'string' ? groupItem : groupItem.value);
            }
            return [item.value];
        })
    );

    const normalizedSearch = (searchValue ?? '').trim();
    const canCreate = (creatable ?? !!onCreate) && normalizedSearch.length > 0 && !baseValues.has(normalizedSearch);
    const createOptionValue = normalizedSearch;
    const dataWithCreate = canCreate
        ? [...baseData, { value: createOptionValue, label: `${addFilterLabel} ${normalizedSearch}` }]
        : baseData;

    return (
        <MultiSelect
            leftSection={<IconSearch size="1.1rem" stroke={1.5} />}
            radius="xl"
            size={size}
            styles={
                () => (
                    theme.focusRing === 'never' ? {
                        searchInput: { lineHeight: 'unset' }, value: { minHeight: '1.4rem' }, defaultValue: { paddingBottom: '0.1rem' }, // Fix the 'ggg' being cut off and vertical alignment
                        input: {
                            '&:focus-within': { outline: 'unset' } // fix focus ring bug
                        }
                    } : {
                        searchInput: { lineHeight: 'unset' }, value: { minHeight: '1.4rem' }, defaultValue: { paddingBottom: '0.1rem' }, // Fix the 'ggg' being cut off and vertical alignment
                    }
                )
            }
            rightSection={
                <ActionIcon size={size} radius="xl" color={theme.primaryColor} variant={iconVariant} onClick={(e) => onSearchClick(e)}>
                    {!isRTL ? (
                        <IconArrowRight size={iconsSize} stroke={1.5} />
                    ) : (
                        <IconArrowLeft size={iconsSize} stroke={1.5} />
                    )}
                </ActionIcon>
            }
            rightSectionWidth={42}
            value={value}
            data={dataWithCreate}

            onChange={onChange}
            onSearchChange={onSearchChange}
            onOptionSubmit={(selectedValue) => {
                onOptionSubmit?.(selectedValue);

                if (canCreate && selectedValue === createOptionValue) {
                    onCreate?.(normalizedSearch);
                }
            }}

            placeholder={placeholder}
            searchable
            data-autofocus={autoFocus}
            autoFocus={autoFocus}
            ref={ref}
            {...rest}
        />
    );
})

