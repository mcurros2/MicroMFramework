import { Button, Group, Stack, Text, Title } from "@mantine/core";
import {
    APPMenuConfigProps, ContextualMenuPanel, createMenuRoute, DataGrid, DataGridPanel, DataMap, DataView, DataViewPanel, EntityFormPanel,
    FilesUploadForm, ImportDataPanel, MenuConfigItem, MenuEntityActionModal, MenuEntityFormModal, MicroMClient, navigateToRoute
} from "@mcurros2/microm";
import {
    IconAddressBook, IconBuildingStore, IconCategory, IconChartBar, IconDatabase, IconFileImport, IconFiles, IconForms, IconHome, IconLayoutCards,
    IconLogout, IconMap, IconMail, IconPlus, IconSettings, IconTags, IconUser, IconWorld
} from "@tabler/icons-react";
import { useMemo } from "react";
import { RequiresGoogleMaps } from "../maps/googleMapsConfig";
import { CustomerCard } from "../mocks/CustomerCard";
import { Branches, Categories, Countries, Customers, Documents, FieldsDemo, Provinces, Tags } from "../mocks/entities";
import { FieldsDemoForm } from "../mocks/FieldsDemoForm";
import { CoreShowcase } from "../showcase/MicroMShowcase";

function DemoFields({ client }: { client: MicroMClient }) {
    const entity = useMemo(() => {
        const e = new FieldsDemo(client);
        e.def.columns.c_demo_id.value = "DEMO1";
        return e;
    }, [client]);
    return <FieldsDemoForm entity={entity} initialFormMode="edit" />;
}

function DemoUploads({ client }: { client: MicroMClient }) {
    const entity = useMemo(() => new Documents(client), [client]);
    return <FilesUploadForm client={client} fileProcessColumn={entity.def.columns.c_fileprocess_id} maxFilesCount={3} editor />;
}

function CustomerPicker({ client }: { client: MicroMClient }) {
    const ids = ["1", "2", "3"];
    return (
        <Stack>
            <Text size="sm">Opens the contextual "customer" menu (ContextualMenuPanel) with the customer as parent keys.</Text>
            <Group>
                {ids.map(id => (
                    <Button key={id} leftIcon={<IconUser size="1rem" />} variant="light" onClick={() => navigateToRoute(createMenuRoute({
                        menuId: "customer",
                        context: { parentKeys: { c_customer_id: id }, contextLabel: `Customer ${id}`, originPath: "/main/sales/workspace" },
                    }))}>Customer {id}</Button>
                ))}
            </Group>
            <Text size="xs" color="dimmed">client: {client.getAPPID()}</Text>
        </Stack>
    );
}

const mapProps = (client: MicroMClient) => {
    const entity = new Customers(client);
    return { entity, viewName: entity.def.views.cus_brwMap.name, selectionMode: "multi" as const, formMode: "edit" as const };
};

// Module level and stable: useMenuContent rebuilds items when the config object changes.
export const navbarMenus: Record<string, MenuConfigItem> = {
    main: {
        isMain: true,
        menu: ({ client, setIsLoggedIn }) => [
            { ID: "home", label: "Home", icon: <IconHome size="1.2rem" />, section: "header", content: <CoreShowcase /> },
            {
                ID: "data", label: "Data", icon: <IconDatabase size="1.2rem" />, section: "items", subitems: [
                    { ID: "customers", label: "Customers", icon: <IconAddressBook size="1.1rem" />, section: "items", content: <DataGrid entity={new Customers(client)} viewName="cus_brwStandard" selectionMode="multi" formMode="edit" gridHeight="calc(100vh - 14rem)" /> },
                    { ID: "categories", label: "Categories", icon: <IconCategory size="1.1rem" />, section: "items", content: <DataGrid entity={new Categories(client)} viewName="cat_brwStandard" selectionMode="multi" formMode="edit" /> },
                    { ID: "countries", label: "Countries", icon: <IconWorld size="1.1rem" />, section: "items", content: <DataGrid entity={new Countries(client)} viewName="cou_brwStandard" selectionMode="multi" formMode="edit" /> },
                    { ID: "provinces", label: "Provinces", icon: <IconMap size="1.1rem" />, section: "items", content: <DataGrid entity={new Provinces(client)} viewName="prv_brwStandard" selectionMode="multi" formMode="edit" /> },
                    { ID: "tags", label: "Tags", icon: <IconTags size="1.1rem" />, section: "items", content: <DataGrid entity={new Tags(client)} viewName="tag_brwStandard" selectionMode="multi" formMode="edit" /> },
                ]
            },
            {
                ID: "views", label: "Views", icon: <IconLayoutCards size="1.2rem" />, section: "items", subitems: [
                    { ID: "cards", label: "Customer cards", icon: <IconLayoutCards size="1.1rem" />, section: "items", content: <DataView entity={new Customers(client)} viewName="cus_brwStandard" selectionMode="multi" Card={CustomerCard} /> },
                    { ID: "map", label: "Customer map", icon: <IconMap size="1.1rem" />, section: "items", content: <RequiresGoogleMaps><DataMap dataGridProps={mapProps(client)} latitudRecordIndex={2} longitudRecordIndex={3} mapHeight="60vh" /></RequiresGoogleMaps> },
                ]
            },
            { ID: "fields", label: "Fields demo", icon: <IconForms size="1.2rem" />, description: "Every *Field", section: "items", content: <DemoFields client={client} /> },
            { ID: "files", label: "Files", icon: <IconFiles size="1.2rem" />, section: "items", content: <DemoUploads client={client} /> },
            { ID: "logout", label: "Log out", icon: <IconLogout size="1.2rem" />, section: "footer", noActive: true, onClick: () => setIsLoggedIn(false) },
        ],
    },
};

export const cardMenus: Record<string, APPMenuConfigProps> = {
    main: {
        isMain: true,
        menu: ({ client }) => [
            {
                ID: "sales", label: "Sales", icon: <IconChartBar size="1.2rem" />, section: "items", description: "Customers and their data", subitems: [
                    { ID: "customers", label: "Customers", icon: <IconAddressBook size="1.2rem" />, section: "items", canShowAsShortcut: true, description: "DataGridPanel", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name }; }} /> },
                    { ID: "cards", label: "Customer cards", icon: <IconLayoutCards size="1.2rem" />, section: "items", canShowAsShortcut: true, description: "DataViewPanel", content: <DataViewPanel client={client} selectionMode="multi" entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name, Card: CustomerCard }; }} /> },
                    { ID: "new", label: "New customer", icon: <IconPlus size="1.2rem" />, section: "items", description: "MenuEntityFormModal", content: <MenuEntityFormModal client={client} entityConstructor={c => new Customers(c)} initialFormMode="add" /> },
                    { ID: "newsletter", label: "Send newsletter", icon: <IconMail size="1.2rem" />, section: "items", description: "MenuEntityActionModal", content: <MenuEntityActionModal client={client} entityConstructor={c => new Customers(c)} actionName="sendNewsletter" /> },
                    { ID: "workspace", label: "Customer workspace", icon: <IconUser size="1.2rem" />, section: "items", description: "ContextualMenuPanel", content: <CustomerPicker client={client} /> },
                ]
            },
            {
                ID: "catalog", label: "Catalog", icon: <IconSettings size="1.2rem" />, section: "items", description: "Lookup tables", subitems: [
                    { ID: "categories", label: "Categories", icon: <IconCategory size="1.2rem" />, section: "items", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => ({ entity: new Categories(c), view: "cat_brwStandard" })} /> },
                    { ID: "tags", label: "Tags", icon: <IconTags size="1.2rem" />, section: "items", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => ({ entity: new Tags(c), view: "tag_brwStandard" })} /> },
                    {
                        ID: "geo", label: "Geography", icon: <IconWorld size="1.2rem" />, section: "items", description: "Nested cards", subitems: [
                            { ID: "countries", label: "Countries", icon: <IconWorld size="1.2rem" />, section: "items", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => ({ entity: new Countries(c), view: "cou_brwStandard" })} /> },
                            { ID: "provinces", label: "Provinces", icon: <IconMap size="1.2rem" />, section: "items", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => ({ entity: new Provinces(c), view: "prv_brwStandard" })} /> },
                            { ID: "branches", label: "Branches", icon: <IconBuildingStore size="1.2rem" />, section: "items", content: <DataGridPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => ({ entity: new Branches(c), view: "bra_brwStandard" })} /> },
                        ]
                    },
                ]
            },
            {
                ID: "tools", label: "Tools", icon: <IconFileImport size="1.2rem" />, section: "items", subitems: [
                    { ID: "import", label: "Import customers", icon: <IconFileImport size="1.2rem" />, section: "items", description: "ImportDataPanel", content: <ImportDataPanel client={client} selectionMode="multi" formMode="edit" entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name }; }} /> },
                    { ID: "fields", label: "Fields demo", icon: <IconForms size="1.2rem" />, section: "items", content: <DemoFields client={client} /> },
                ]
            },
        ],
    },
    customer: {
        hostPanel: ContextualMenuPanel,
        menu: ({ client }) => [
            {
                ID: "profile", label: "Profile", icon: <IconUser size="1.2rem" />, section: "items", description: "EntityFormPanel (edit)",
                content: ({ parentKeys }) => <EntityFormPanel client={client} parentKeys={parentKeys} initialFormMode="edit" entityConstructor={(c, pk) => new Customers(c, pk)} />,
            },
            {
                ID: "summary", label: "Summary", icon: <IconChartBar size="1.2rem" />, section: "items",
                content: ({ parentKeys }) => <Stack><Title order={4}>Customer {String(parentKeys.c_customer_id)}</Title><Text size="sm">Content resolved with the route context parent keys.</Text></Stack>,
            },
        ],
    },
};
