import { ActionIcon, MantineColor, MantineNumberSize, Sx, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ForwardedRef, ReactNode, forwardRef, useState } from "react";
import { ActionIconVariant } from "../Core";


export interface ToggleActionIconOptions {
    hidden: boolean,
    size: string,
    onColor: MantineColor,
    offColor: MantineColor,
    onVariant?: ActionIconVariant,
    offVariant?: ActionIconVariant,
    onIcon: ReactNode,
    offIcon: ReactNode,
    initialStatus?: 'on' | 'off',
    onClick?: () => void,
    radius?: MantineNumberSize,
    title?: string
}

const defaultProps: Partial<ToggleActionIconOptions> = {
    onVariant: 'light',
    offVariant: 'light',
    initialStatus: 'off',
    radius: 'xl'
}

export const ToggleActionIcon = forwardRef(function ToggleActionIcon(props: ToggleActionIconOptions, ref: ForwardedRef<HTMLButtonElement>) {
    const {
        hidden, size, onColor, offColor, onVariant, offVariant, onIcon, offIcon, initialStatus, onClick, radius, title
    } = useComponentDefaultProps('ToggleActionIcon', defaultProps, props);

    const theme = useMantineTheme();

    const [onOff, setOnOff] = useState<boolean>(initialStatus === "on");

    const visibility: Sx = hidden ? {
        visibility: "hidden",
        ":focus": { outlineColor: theme.fn.themeColor(onOff ? onColor : offColor) }
    } : {
        ":focus": { outlineColor: theme.fn.themeColor(onOff ? onColor : offColor) }
    }

    const onClickHandler = () => {
        setOnOff(prev => !prev);
        if (onClick) onClick();
    }

    return (
        <ActionIcon
            title={title}
            ref={ref}
            sx={visibility}
            onClick={() => onClickHandler()}
            size={size}
            radius={radius}
            color={onOff ? onColor : offColor}
            variant={onOff ? onVariant : offVariant}>
            {onOff ? onIcon : offIcon}
        </ActionIcon>
    )
})