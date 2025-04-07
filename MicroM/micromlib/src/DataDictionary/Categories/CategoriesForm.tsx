import { Card, Group, Stack, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { DataGrid, EntityForm, FormOptions, Lookup, useEnterAsTab, useEntityForm } from "../../UI";
import { Categories } from "./Categories";
import { CategoriesValues } from "./CategoriesValues";



export const CategoriesFormDefaultProps: Partial<FormOptions<Categories>> = {
    initialFormMode: 'view'
}

export function CategoriesForm(props: FormOptions<Categories>) {
    const { entity, initialFormMode, getDataOnInit } = useComponentDefaultProps('CategoriesValuesForm', CategoriesFormDefaultProps, props);

    const useEnterAsTabRef = useEnterAsTab<HTMLDivElement>();

    const theme = useMantineTheme();

    const formAPI = useEntityForm(
        {
            entity: entity,
            initialFormMode: initialFormMode,
            validateInputOnBlur: true,
            getDataOnInit: getDataOnInit!
        }
    );

    const cav = new CategoriesValues(entity.API.client);

    return (
        <EntityForm formAPI={formAPI}>
            <Stack ref={useEnterAsTabRef}>
                <Group>
                    <Lookup
                        entityForm={formAPI}
                        entity={entity}
                        lookupDefName={entity.def.lookups.Categories.name}
                        autoFocus
                        column={entity.def.columns.c_category_id}
                        parentKeys={{}}
                        required={false}
                    />
                </Group>
                <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
                    <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                        <Text weight="500">{entity.Title}</Text>
                    </Card.Section>
                    <DataGrid entity={cav} parentKeys={{}} viewName={cav.def.views.cav_brwStandard.name} refreshOnInit={true} limit="10000" selectionMode="multi" key="datagrid" />
                </Card>
            </Stack>
        </EntityForm>
    )

}