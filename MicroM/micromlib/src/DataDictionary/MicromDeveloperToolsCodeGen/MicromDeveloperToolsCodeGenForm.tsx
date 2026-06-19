import { Card, Tabs, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconBrandTypescript, IconFileTypeSql } from "@tabler/icons-react";
import { CodeBlock, EntityForm, FormOptions, useEntityForm } from "../../UI";
import { MicromDeveloperToolsCodeGen } from "./MicromDeveloperToolsCodeGen";

export const MicromDeveloperToolsCodeGenFormDefaultProps: Partial<FormOptions<MicromDeveloperToolsCodeGen>> = {
    initialFormMode: "view"
}

export function MicromDeveloperToolsCodeGenForm(props: FormOptions<MicromDeveloperToolsCodeGen>) {
    const { entity, initialFormMode, getDataOnInit, onSaved, onCancel } = useComponentDefaultProps('MicromDeveloperToolsCodeGenForm', MicromDeveloperToolsCodeGenFormDefaultProps, props);
    const formAPI = useEntityForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel });

    const theme = useMantineTheme();

    const cols = entity.def.columns;

    return (
        <EntityForm formAPI={formAPI}>
            <Card bg={theme.colorScheme === 'dark' ? theme.colors.dark[9] : undefined}>
                <Tabs defaultValue={cols.vc_table.value ? "sqltable" : "reactdef"}>
                    <Tabs.List>
                        {cols.vc_table.value && <Tabs.Tab value="sqltable" icon={<IconFileTypeSql />}>create_table.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqlupdate" icon={<IconFileTypeSql />}>update.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqliupdate" icon={<IconFileTypeSql />}>iupdate.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqldrop" icon={<IconFileTypeSql />}>drop.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqlidrop" icon={<IconFileTypeSql />}>idrop.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqlget" icon={<IconFileTypeSql />}>get.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqlbrwstandard" icon={<IconFileTypeSql />}>brwStandard.sql</Tabs.Tab>}
                        {cols.vc_table.value && <Tabs.Tab value="sqllookup" icon={<IconFileTypeSql />}>lookup.sql</Tabs.Tab>}
                        {cols.vc_custom_procs.value &&
                            <Tabs.Tab value="sqlcustom" icon={<IconFileTypeSql />}>custom_procs.sql</Tabs.Tab>
                        }
                        <Tabs.Tab value="reactdef" icon={<IconBrandTypescript />}>definition.tsx</Tabs.Tab>
                        <Tabs.Tab value="reactentity" icon={<IconBrandTypescript />}>entity.tsx</Tabs.Tab>
                        <Tabs.Tab value="reactform" icon={<IconBrandTypescript />}>form.tsx</Tabs.Tab>
                        {cols.vc_react_categories.value &&
                            <Tabs.Tab value="reactcategories" icon={<IconBrandTypescript />}>categories.tsx</Tabs.Tab>
                        }
                    </Tabs.List>
                    {cols.vc_table.value &&
                        <Tabs.Panel value="sqltable" mt="xs">
                            <CodeBlock language="sql" codeText={`${cols.vc_table.value}${cols.vc_indexes.value ? `\nGO\n${cols.vc_indexes.value}` : ''}`} />
                        </Tabs.Panel>
                    }

                    {cols.vc_table.value && <Tabs.Panel value="sqlupdate" mt="xs"><CodeBlock language="sql" codeText={cols.vc_sp_update.value} /></Tabs.Panel>}
                    {cols.vc_table.value &&
                        <Tabs.Panel value="sqliupdate" mt="xs">
                            <CodeBlock language="sql" codeText={`${cols.vc_sp_iupdate.value}\nGO${cols.vc_sp_updatei.value}`} />
                        </Tabs.Panel>
                    }
                    {cols.vc_table.value && <Tabs.Panel value="sqldrop" mt="xs"><CodeBlock language="sql" codeText={cols.vc_sp_drop.value} /></Tabs.Panel>}
                    {cols.vc_table.value &&
                        <Tabs.Panel value="sqlidrop" mt="xs">
                            <CodeBlock language="sql" codeText={`${cols.vc_sp_idrop.value}\nGO${cols.vc_sp_dropi.value}`} />
                        </Tabs.Panel>
                    }
                    {cols.vc_table.value && <Tabs.Panel value="sqlget" mt="xs"><CodeBlock language="sql" codeText={cols.vc_sp_get.value} /></Tabs.Panel>}
                    {cols.vc_table.value && <Tabs.Panel value="sqlbrwstandard" mt="xs"><CodeBlock language="sql" codeText={cols.vc_sp_brwStandard.value} /></Tabs.Panel>}
                    {cols.vc_table.value && <Tabs.Panel value="sqllookup" mt="xs"><CodeBlock language="sql" codeText={cols.vc_sp_lookup.value} /></Tabs.Panel>}
                    {cols.vc_custom_procs.value &&
                        <Tabs.Panel value="sqlcustom" mt="xs"><CodeBlock language="sql" codeText={cols.vc_custom_procs.value} /></Tabs.Panel>
                    }
                    <Tabs.Panel value="reactdef" mt="xs"><CodeBlock language="tsx" codeText={cols.vc_react_definition.value} /></Tabs.Panel>
                    <Tabs.Panel value="reactentity" mt="xs"><CodeBlock language="tsx" codeText={cols.vc_react_entity.value} /></Tabs.Panel>
                    {cols.vc_react_categories.value &&
                        <Tabs.Panel value="reactcategories" mt="xs"><CodeBlock language="tsx" codeText={cols.vc_react_categories.value} /></Tabs.Panel>
                    }
                    <Tabs.Panel value="reactform" mt="xs"><CodeBlock language="tsx" codeText={cols.vc_react_form.value} /></Tabs.Panel>
                </Tabs>
            </Card>
        </EntityForm>
    )
}