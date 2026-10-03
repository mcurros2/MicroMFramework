import { Value, ValuesObject } from "@mcurros2/microm";
import { Branches, Categories, Countries, Customers, FieldsDemo, Provinces, Tags } from "./entities";
import { MockMicroMClient, MockMicroMClientOptions, MockViewColumn } from "./MockMicroMClient";

// Fixed seed so stories render the same fictitious data every time.
function seeded(seed: number) {
    let s = seed >>> 0;
    return () => {
        s = (s + 0x6D2B79F5) >>> 0;
        let t = s;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

const rnd = seeded(20260930);
const pick = <T,>(arr: T[]) => arr[Math.floor(rnd() * arr.length)];
const int = (min: number, max: number) => Math.floor(rnd() * (max - min + 1)) + min;

const FIRST_NAMES = ["Alice", "Bruce", "Carol", "David", "Emma", "Frank", "Grace", "Henry", "Irene", "Jack", "Laura", "Mark", "Nora", "Oscar", "Paula", "Ryan", "Sarah", "Tom", "Vera", "Will"];
const LAST_NAMES = ["Sample", "Example", "Test", "Demo", "Mock", "Fixture", "Stub", "Draft", "Placeholder", "Template"];

export const CATEGORIES: ValuesObject[] = [
    { c_category_id: "WHOLESALE", vc_description: "Wholesale" },
    { c_category_id: "RETAIL", vc_description: "Retail" },
    { c_category_id: "CORPORATE", vc_description: "Corporate" },
    { c_category_id: "GOVERNMENT", vc_description: "Government" },
    { c_category_id: "EDUCATION", vc_description: "Education" },
];

const TAGS: ValuesObject[] = [
    { c_tag_id: "VIP", vc_description: "VIP" },
    { c_tag_id: "NEW", vc_description: "New" },
    { c_tag_id: "LATE", vc_description: "Late payer" },
    { c_tag_id: "EXPORT", vc_description: "Export" },
    { c_tag_id: "ONLINE", vc_description: "Online only" },
];

const COUNTRIES: ValuesObject[] = [
    { c_country_id: "AR", vc_country: "Argentina" },
    { c_country_id: "UY", vc_country: "Uruguay" },
    { c_country_id: "CL", vc_country: "Chile" },
];

// Province centers are used to scatter fictitious coordinates for the map stories.
const PROVINCES: (ValuesObject & { lat: number, lng: number })[] = [
    { c_country_id: "AR", c_province_id: "BA", vc_province: "Buenos Aires", lat: -34.61, lng: -58.38 },
    { c_country_id: "AR", c_province_id: "CB", vc_province: "Cordoba", lat: -31.42, lng: -64.18 },
    { c_country_id: "AR", c_province_id: "MZ", vc_province: "Mendoza", lat: -32.89, lng: -68.83 },
    { c_country_id: "AR", c_province_id: "SF", vc_province: "Santa Fe", lat: -32.95, lng: -60.66 },
    { c_country_id: "UY", c_province_id: "MO", vc_province: "Montevideo", lat: -34.90, lng: -56.16 },
    { c_country_id: "UY", c_province_id: "CA", vc_province: "Canelones", lat: -34.52, lng: -56.28 },
    { c_country_id: "CL", c_province_id: "RM", vc_province: "Santiago", lat: -33.45, lng: -70.66 },
    { c_country_id: "CL", c_province_id: "VA", vc_province: "Valparaiso", lat: -33.05, lng: -71.62 },
];

export function categoryDescription(id: unknown) {
    return CATEGORIES.find(c => c.c_category_id === id)?.vc_description ?? null;
}

function isoDate(daysAgo: number) {
    const d = new Date(Date.UTC(2026, 8, 30));
    d.setUTCDate(d.getUTCDate() - daysAgo);
    return d.toISOString().substring(0, 10) + "T00:00:00";
}

const jitter = (v: number, amount: number) => Math.round((v + (rnd() - 0.5) * amount) * 1e6) / 1e6;

function buildCustomers(count: number): ValuesObject[] {
    return Array.from({ length: count }, (_, i) => {
        const name = `${pick(FIRST_NAMES)} ${pick(LAST_NAMES)}`;
        const province = pick(PROVINCES);
        const tags = TAGS.filter(() => rnd() > 0.75).map(t => t.c_tag_id as string);
        return {
            c_customer_id: String(i + 1),
            vc_name: name,
            vc_email: `${name.toLowerCase().replace(/[^a-z]+/g, ".")}${i + 1}@example.com`,
            vc_phone: `+1555010${String(int(1000, 9999))}`,
            c_category_id: pick(CATEGORIES).c_category_id,
            c_country_id: province.c_country_id,
            c_province_id: province.c_province_id,
            vc_tags: tags.length ? tags : null,
            dt_since: isoDate(int(0, 1500)),
            m_credit_limit: int(50, 5000) * 100,
            bt_active: rnd() > 0.2,
            m_latitude: jitter(province.lat, 0.6),
            m_longitude: jitter(province.lng, 0.6),
            vc_notes: rnd() > 0.6 ? "Sample record generated for Storybook." : null,
        };
    });
}

export const customersRows = buildCustomers(120);

const BRANCHES: ValuesObject[] = PROVINCES.map((p, i) => ({
    c_branch_id: `B${String(i + 1).padStart(2, "0")}`,
    vc_name: `${p.vc_province} branch`,
    vc_address: `${100 + i * 17} Sample Street`,
    m_latitude: jitter(p.lat, 0.2),
    m_longitude: jitter(p.lng, 0.2),
}));

const raw = (names: string[], types: MockViewColumn["type"][]): MockViewColumn[] => names.map((name, i) => ({ name, header: name, type: types[i] }));

function registerSystemTables(client: MockMicroMClient) {
    client.registerTable({
        entityName: "FileStoreProcess", pk: ["c_fileprocess_id"], autonumColumn: "c_fileprocess_id", descriptionColumn: "c_fileprocess_id",
        viewColumns: raw(["c_fileprocess_id"], ["char"]), rows: [],
    });
    client.registerTable({
        entityName: "FileStoreClient", pk: ["vc_fileguid"], descriptionColumn: "vc_filename", filterBy: ["c_fileprocess_id"],
        viewColumns: raw(["vc_fileguid", "vc_filename", "bi_filesize", "c_fileuploadstatus_id"], ["varchar", "varchar", "bigint", "char"]), rows: [],
    });
    client.registerTable({
        entityName: "ImportProcess", pk: ["c_import_process_id"], descriptionColumn: "c_import_process_id", filterBy: ["vc_assemblytypename"],
        viewColumns: [
            { name: "c_import_process_id", header: "Import", type: "char" },
            { name: "c_fileprocess_id", header: "File process", type: "char" },
            { name: "i_total_records", header: "Records", type: "int" },
            { name: "i_errors", header: "Errors", type: "int" },
            { name: "vc_assemblytypename", header: "Entity", type: "varchar" },
            { name: "vc_import_procname", header: "Procedure", type: "varchar" },
            { name: "c_import_status_id", header: "Status", type: "char" },
            { name: "vc_fileguid", header: "File", type: "varchar" },
        ],
        rows: [{
            c_import_process_id: "1", c_fileprocess_id: "", i_total_records: 25, i_errors: 2, vc_assemblytypename: "Customers",
            vc_import_procname: null, c_import_status_id: "Completed", vc_fileguid: "",
        }],
    });
    client.registerTable({
        entityName: "ImportProcessErrors", pk: ["c_import_process_error_id"], descriptionColumn: "vc_error", filterBy: ["c_import_process_id"],
        viewColumns: [
            { name: "c_import_process_error_id", header: "Error", type: "char" },
            { name: "c_import_process_id", header: "Import", type: "char" },
            { name: "i_row_number", header: "Row", type: "int" },
            { name: "vc_error", header: "Message", type: "varchar" },
        ],
        rows: [
            { c_import_process_error_id: "1", c_import_process_id: "1", i_row_number: 7, vc_error: "Email is not valid" },
            { c_import_process_error_id: "2", c_import_process_id: "1", i_row_number: 19, vc_error: "Category does not exist" },
        ],
    });
    client.registerTable({
        entityName: "MicromEntitiesTypes", pk: ["vc_entity_name"], descriptionColumn: "vc_entity_name",
        viewColumns: [{ name: "vc_entity_name", header: "Entity", type: "varchar" }, { name: "vc_entity_type", header: "Type", type: "varchar" }],
        rows: ["Customers", "Categories", "Countries", "Provinces", "Tags", "Branches"].map(n => ({ vc_entity_name: n, vc_entity_type: `Storybook.${n}` })),
    });
    const code = (what: string, name: string) => `-- ${what} for ${name} (mock)\n-- generated by MockMicroMClient`;
    client.registerTable({
        entityName: "MicromDeveloperToolsCodeGen", pk: ["vc_classname"], descriptionColumn: "vc_classname", viewColumns: [],
        rows: ["Customers", "Categories", "Countries", "Provinces", "Tags", "Branches"].map(n => ({
            vc_classname: n,
            ...Object.fromEntries(["vc_table", "vc_indexes", "vc_sp_get", "vc_sp_update", "vc_sp_iupdate", "vc_sp_updatei", "vc_sp_drop", "vc_sp_idrop",
                "vc_sp_dropi", "vc_sp_lookup", "vc_sp_brwStandard", "vc_custom_procs", "vc_react_definition", "vc_react_entity", "vc_react_categories",
                "vc_react_form"].map(k => [k, code(k, n)])),
        })),
    });
}

export interface CreateMockClientOptions extends MockMicroMClientOptions {
    customers?: number,
}

// Each story can create its own client so inserts/updates/deletes are not shared between stories.
export function createMockClient(options?: CreateMockClientOptions) {
    const client = new MockMicroMClient({ latency: 350, ...options });

    const lookupTable = (entityName: string, pk: string, desc: string, rows: ValuesObject[], entity: { def: { columns: Record<string, unknown> } }) => {
        client.registerTable({
            entityName, pk: [pk], descriptionColumn: desc, rows: rows.map(r => ({ ...r })),
            viewColumns: MockMicroMClient.viewColumnsFrom(entity as never, [pk, desc]),
        });
    };

    lookupTable("Categories", "c_category_id", "vc_description", CATEGORIES, new Categories(client));
    lookupTable("Tags", "c_tag_id", "vc_description", TAGS, new Tags(client));
    lookupTable("Countries", "c_country_id", "vc_country", COUNTRIES, new Countries(client));

    const categories = client.table("Categories")!.rows;
    const countries = client.table("Countries")!.rows;
    const desc = (rows: ValuesObject[], key: string, value: Value, field: string) => rows.find(r => r[key] === value)?.[field] ?? null;

    client.registerTable({
        entityName: new Provinces(client).name,
        pk: ["c_country_id", "c_province_id"],
        descriptionColumn: "vc_province",
        filterBy: ["c_country_id"],
        viewColumns: [
            { name: "c_country_id", header: "Country", type: "char" },
            { name: "c_province_id", header: "Province", type: "char" },
            { name: "key", header: "Key", type: "varchar", compute: r => `${r.c_country_id}-${r.c_province_id}` },
            { name: "vc_province", header: "Name", type: "varchar" },
        ],
        rows: PROVINCES.map(({ lat: _lat, lng: _lng, ...r }) => ({ ...r })),
    });

    const cus = new Customers(client);
    client.registerTable({
        entityName: cus.name,
        pk: ["c_customer_id"],
        autonumColumn: "c_customer_id",
        descriptionColumn: "vc_name",
        filterBy: [{ param: "c_category_filter", column: "c_category_id" }, { param: "c_country_filter", column: "c_country_id" }],
        viewColumns: [
            ...MockMicroMClient.viewColumnsFrom(cus, ["c_customer_id", "vc_name", "vc_email", "vc_phone"]),
            { name: "vc_category", header: "Category", type: "varchar", compute: r => desc(categories, "c_category_id", r.c_category_id, "vc_description") },
            { name: "vc_country", header: "Country", type: "varchar", compute: r => desc(countries, "c_country_id", r.c_country_id, "vc_country") },
            ...MockMicroMClient.viewColumnsFrom(cus, ["dt_since", "m_credit_limit", "bt_active"]),
        ],
        views: {
            cus_brwMap: [
                { name: "c_customer_id", header: "Customer", type: "char" },
                { name: "vc_name", header: "Full name", type: "varchar" },
                { name: "m_latitude", header: "Latitude", type: "decimal" },
                { name: "m_longitude", header: "Longitude", type: "decimal" },
                { name: "vc_category", header: "Category", type: "varchar", compute: r => desc(categories, "c_category_id", r.c_category_id, "vc_description") },
            ],
        },
        rows: (options?.customers ? buildCustomers(options.customers) : customersRows).map(r => ({ ...r })),
    });

    const bra = new Branches(client);
    client.registerTable({
        entityName: bra.name, pk: ["c_branch_id"], descriptionColumn: "vc_name",
        viewColumns: MockMicroMClient.viewColumnsFrom(bra, ["c_branch_id", "vc_name", "m_latitude", "m_longitude", "vc_address"]),
        rows: BRANCHES.map(r => ({ ...r })),
    });

    const demo = new FieldsDemo(client);
    client.registerTable({
        entityName: demo.name,
        pk: ["c_demo_id"],
        descriptionColumn: "vc_text",
        viewColumns: MockMicroMClient.viewColumnsFrom(demo, ["c_demo_id", "vc_text", "vc_email", "i_quantity", "m_amount", "dt_date", "bt_terms"]),
        rows: [{
            c_demo_id: "DEMO1",
            vc_text: "Sample text",
            vc_email: "demo@example.com",
            vc_phone: "+15550101234",
            vc_url: "https://example.com",
            vc_cuit: "20-12345678-6",
            vc_password: null,
            i_quantity: 42,
            m_amount: 12345.67,
            m_budget: 250000,
            bi_quota: 5368709120,
            dt_date: isoDate(10),
            t_time: "09:30",
            dt_week_start: isoDate(2),
            dt_week_end: isoDate(-4),
            bt_terms: true,
            bt_notifications: false,
            c_priority: "MEDIUM",
            c_category_id: "EDUCATION",
            c_category2_id: "RETAIL",
            vc_tags: ["VIP", "EXPORT"],
            c_country_id: "AR",
            c_province_id: "CB",
            vc_pin: null,
            vc_notes: "These notes come from the mock.\nSecond line.",
        }],
    });

    registerSystemTables(client);

    return client;
}
