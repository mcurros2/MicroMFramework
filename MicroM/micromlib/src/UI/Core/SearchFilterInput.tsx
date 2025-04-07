import { ActionIcon, MultiSelect, MultiSelectProps, useComponentDefaultProps, useMantineTheme } from '@mantine/core';
import { IconArrowLeft, IconArrowRight, IconSearch } from '@tabler/icons-react';
import { ActionIconVariant } from '../Core';

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

export function SearchFilterInput(props: SearchFilterInputProps) {
    const {
        onSearchClick, size, iconsSize, value, onChange, placeholder, autoFocus, iconVariant, addFilterLabel,
        onSearchChange, onCreate,
        ...rest
    } = useComponentDefaultProps('SearchFilterInput', SearchFilterInputDefaultProps, props);

    const theme = useMantineTheme();

    return (
        <MultiSelect
            icon={<IconSearch size="1.1rem" stroke={1.5} />}
            radius="xl"
            size={size}
            styles={
                () => ({
                    searchInput: { lineHeight: 'unset' }, value: { minHeight: '1.4rem' }, defaultValue: { paddingBottom: '0.1rem' } }) // Fix the 'ggg' being cut off and vertical alignment
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
            {...rest}
        />
    );
}