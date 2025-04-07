import { MantineColor, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ReactNode, forwardRef } from "react";

export interface CircleFilledIconProps {
    backColor?: MantineColor,
    color?: MantineColor,
    width?: string | number,
    minWidth?: string | number,
    icon: ReactNode,
    mr?: string
}

export const CircleFilledIconDefaultProps: Partial<CircleFilledIconProps> = {
    backColor: "green",
    width: "1.75rem",
    mr: "md"
}

export const CircleFilledIcon = forwardRef<HTMLInputElement, CircleFilledIconProps>(function CircleFilledIcon(props: CircleFilledIconProps, ref) {
    const {
        backColor, color, width, icon, mr, minWidth
    } = useComponentDefaultProps('CircleFilledIcon', CircleFilledIconDefaultProps, props);

    const theme = useMantineTheme();

    const iconColor = color ?? theme.white;


    return (
        <div ref={ref}
            style={{
                boxSizing: 'border-box',
                marginRight: theme.spacing[mr ?? "md"],
                width: width,
                height: width,
                borderRadius: width,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: backColor,
                color: iconColor,
                minWidth: minWidth ?? width
            }}>
            {icon}
        </div>
    );
});