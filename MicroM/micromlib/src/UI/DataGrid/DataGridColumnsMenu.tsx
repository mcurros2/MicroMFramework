import { Menu, ScrollArea } from "@mantine/core";
import { IconCircleCheck, IconCircleDashedCheck } from "@tabler/icons-react";
import { GridColumn } from "../Grid/Grid";

export interface DataGridColumnsMenuProps {
    columns?: GridColumn[],
    setColumns: (columns: GridColumn[]) => void,
    setOpened: (opened: boolean) => void,
}
export function DataGridColumnsMenu(props: DataGridColumnsMenuProps) {
    const { columns, setOpened, setColumns } = props;

    return (
        <Menu.Dropdown>
            <ScrollArea h="20rem">
                {columns?.map((column, index) => {
                    return (
                        <Menu.Item
                            key={`gridColumnsMenu-${index}`}
                            icon={column.hidden ? <IconCircleDashedCheck size="1rem" /> : <IconCircleCheck size="1rem" />}
                            onClick={(e) => {
                                column.hidden = !column.hidden;
                                setColumns([...columns]);
                                //setOpened(false);
                            }}
                        >
                            {column.text}
                        </Menu.Item>
                    )
                })}
            </ScrollArea>
        </Menu.Dropdown>
    )
}
