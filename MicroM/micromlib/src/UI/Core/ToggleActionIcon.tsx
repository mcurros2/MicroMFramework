import { ActionIcon, MantineColor, MantineSize, useProps } from "@mantine/core";
import { CSSProperties, ForwardedRef, forwardRef, ReactNode, useState } from "react";
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
    radius?: MantineSize,
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
    } = useProps('ToggleActionIcon', defaultProps, props);

    const [onOff, setOnOff] = useState<boolean>(initialStatus === "on");

    const visibility: CSSProperties | undefined = hidden ? { visibility: "hidden" } : undefined;

    const onClickHandler = () => {
        setOnOff(prev => !prev);
        if (onClick) onClick();
    }

    return (
        <ActionIcon
            title={title}
            ref={ref}
            style={visibility}
            onClick={() => onClickHandler()}
            size={size}
            radius={radius}
            color={onOff ? onColor : offColor}
            variant={onOff ? onVariant : offVariant}>
            {onOff ? onIcon : offIcon}
        </ActionIcon>
    )
})

