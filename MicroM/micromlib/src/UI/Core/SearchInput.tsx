import { ActionIcon, TextInput, TextInputProps, useDirection, useProps, useMantineTheme } from '@mantine/core';
import { IconArrowLeft, IconArrowRight, IconSearch } from '@tabler/icons-react';
import { ActionIconVariant } from '../Core';

export interface SearchInputProps extends TextInputProps {
    onSearchClick: React.MouseEventHandler,
    size: string,
    iconsSize: string,
    value: string,
    onChange: React.ChangeEventHandler<HTMLInputElement>,
    placeholder?: string,
    autoFocus?: boolean,
    iconVariant?: ActionIconVariant,
    [x: string]: any
}


export const SearchInputDefaultProps: Partial<SearchInputProps> = {
    iconVariant: "light",
    placeholder: "Enter text to search"
}

export function SearchInput(props: SearchInputProps) {
    const { onSearchClick, size, iconsSize, value, onChange, placeholder, autoFocus, iconVariant, ...rest } = useProps('SearchInput', SearchInputDefaultProps, props);

    const theme = useMantineTheme();
    const { dir } = useDirection();
    const isRTL = dir === 'rtl';

    return (
        <TextInput
            leftSection={<IconSearch size="1.1rem" stroke={1.5} />}
            radius="xl"
            size={size}
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
            onChange={onChange}
            placeholder={placeholder}
            data-autofocus={autoFocus}
            {...rest}
        />
    );
}

