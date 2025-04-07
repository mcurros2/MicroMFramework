import { ReactElement, ReactNode } from "react"

export interface MenuItem {
    ID: string,
    link?: string,
    label: string,
    labelComponent?: ReactNode,
    icon?: ReactNode,
    description?: string,
    notifications?: number,
    subitems?: MenuItem[],
    rightSection?: ReactElement,
    noActive?: boolean,
    loadingComponent?: ReactNode,
    content?: ReactNode | Promise<ReactNode>
    onClick?: () => void,
    section: 'header' | 'items' | 'footer'
}