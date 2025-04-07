import { CheckIcon, ColorSwatch, DefaultMantineColor, Group, Popover, rem, useMantineTheme } from '@mantine/core';
import { IconColorPicker } from '@tabler/icons-react';
import { useState } from 'react';

export interface ColorConfigurationValue {
    colorKey: DefaultMantineColor,
    colorShade: any
}

export interface ColorConfigurationProps {
    onChange(color: ColorConfigurationValue): void;
    value: ColorConfigurationValue;
}

export function ColorConfiguration({ onChange, value }: ColorConfigurationProps) {
    const [opened, setOpened] = useState(false);
    const theme = useMantineTheme();

    const colorArray = Object.entries(theme.colors)
        .flatMap(([colorKey, colorValues]) =>
            colorValues.map((colorValue, colorIndex) => ({
                colorKey,
                colorIndex,
                colorValue,
            }))
        );


    const swatches = colorArray.map(({ colorKey, colorIndex, colorValue }) => (
        <ColorSwatch
            component="button"
            type="button"
            onClick={() => onChange({ colorKey, colorShade: colorIndex })}
            key={`${colorKey}${colorIndex}`}
            color={colorValue}
            size={22}
            style={{ color: theme.white, cursor: 'pointer' }}
            title={`${colorKey}.${colorIndex}`}
        >
            {value.colorKey === colorKey && value.colorShade === colorIndex && <CheckIcon width={rem(10)} />}
        </ColorSwatch>
    ));

    return (
        <Popover
            opened={opened}
            onClose={() => setOpened(false)}
            transitionProps={{ duration: 0 }}
            width={350}
            position="bottom-end"
            withinPortal={true}
            withArrow
        >
            <Popover.Target>
                <ColorSwatch
                    component="button"
                    type="button"
                    color={theme.colors[value.colorKey][value.colorShade] ? theme.colors[value.colorKey][value.colorShade] : theme.colors[theme.primaryColor][6]}
                    onClick={() => setOpened((o) => !o)}
                    size={22}
                    style={{ display: 'block', cursor: 'pointer' }}
                >
                    <IconColorPicker size="0.9rem" color="#fff" />
                </ColorSwatch>
            </Popover.Target>
            <Popover.Dropdown>
                <Group spacing="xs">{swatches}</Group>
            </Popover.Dropdown>
        </Popover>
    );
}