import { ActionIcon, MultiSelect, MultiSelectProps, useComponentDefaultProps, useMantineTheme } from '@mantine/core';
import { IconArrowLeft, IconArrowRight, IconSearch } from '@tabler/icons-react';
import { ActionIconVariant } from '../Core';
import { forwardRef } from 'react';

export interface SearchFilterInputProps extends MultiSelectProps {
    onSearchClick: React.MouseEventHandler,
    size: string,
    iconsSize: string,
    placeholder?: string,
    autoFocus?: boolean,
    addFilterLabel?: string,
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
        onSearchChange, onCreate,
        ...rest
    } = useComponentDefaultProps('SearchFilterInput', SearchFilterInputDefaultProps, props);

    const theme = useMantineTheme();

    // Mantine has a bug in thsi component:
    // When all the items are selected and the multiselect is in a modal and it has autofocus
    // you will need to press escape two time to close the modal
    // this is because it uses a popover with closeOnEscape and it traps escape even if the list is not shown

    return (
        <MultiSelect
            icon={<IconSearch size="1.1rem" stroke={1.5} />}
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
                    {theme.dir === 'ltr' ? (
                        <IconArrowRight size={iconsSize} stroke={1.5} />
                    ) : (
                        <IconArrowLeft size={iconsSize} stroke={1.5} />
                    )}
                </ActionIcon>
            }
            rightSectionWidth={42}
            value={value}

            onChange={onChange}
            onSearchChange={onSearchChange}
            onCreate={onCreate}

            placeholder={placeholder}
            creatable
            searchable
            getCreateLabel={(newValue) => `${addFilterLabel} ${newValue}`}
            data-autofocus={autoFocus}
            autoFocus={autoFocus}
            ref={ref}
            {...rest}
        />
    );
})