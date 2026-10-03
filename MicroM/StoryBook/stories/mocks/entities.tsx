import { Stack, Text } from "@mantine/core";
import {
    CommonFlags as c, DefaultColumns, Entity, EntityClientAction, EntityColumn, EntityColumnFlags as f, EntityDefinition, MicroMClient, NotifySuccess,
    ValuesObject
} from "@mcurros2/microm";
import { IconAddressBook, IconBuildingStore, IconCategory, IconCheck, IconFile, IconFilter, IconForms, IconMail, IconMap, IconTags, IconWorld } from "@tabler/icons-react";

const lookup = (name: string, ctor: (client: MicroMClient, parentKeys?: ValuesObject) => Entity<EntityDefinition>, extra?: object) => ({
    name,
    viewMapping: { keyIndex: 0, descriptionIndex: 1 },
    entityConstructor: ctor,
    ...extra,
});

// ---- Categories ----

export class CategoriesDef extends EntityDefinition {
    columns = {
        c_category_id: new EntityColumn<string>({ name: 'c_category_id', type: 'char', length: 20, flags: c.PK, prompt: 'Category' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Description' }),
        ...DefaultColumns()
    };
    views = { cat_brwStandard: { name: 'cat_brwStandard', keyMappings: { c_category_id: 0 } } };
    constructor() { super('Categories'); }
}

export class Categories extends Entity<CategoriesDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new CategoriesDef(), parentKeys);
        this.Title = "Categories";
        this.Icon = IconCategory;
        this.Form = "AutoForm";
    }
}

// ---- Tags (LookupMultiSelect) ----

export class TagsDef extends EntityDefinition {
    columns = {
        c_tag_id: new EntityColumn<string>({ name: 'c_tag_id', type: 'char', length: 20, flags: c.PK, prompt: 'Tag' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Description' }),
        ...DefaultColumns()
    };
    views = { tag_brwStandard: { name: 'tag_brwStandard', keyMappings: { c_tag_id: 0 } } };
    constructor() { super('Tags'); }
}

export class Tags extends Entity<TagsDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new TagsDef(), parentKeys);
        this.Title = "Tags";
        this.Icon = IconTags;
        this.Form = "AutoForm";
    }
}

// ---- Countries / Provinces (CompoundLookup) ----

export class CountriesDef extends EntityDefinition {
    columns = {
        c_country_id: new EntityColumn<string>({ name: 'c_country_id', type: 'char', length: 3, flags: c.PK, prompt: 'Country' }),
        vc_country: new EntityColumn<string>({ name: 'vc_country', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Name' }),
        ...DefaultColumns()
    };
    views = { cou_brwStandard: { name: 'cou_brwStandard', keyMappings: { c_country_id: 0 } } };
    constructor() { super('Countries'); }
}

export class Countries extends Entity<CountriesDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new CountriesDef(), parentKeys);
        this.Title = "Countries";
        this.Icon = IconWorld;
        this.Form = "AutoForm";
    }
}

export class ProvincesDef extends EntityDefinition {
    columns = {
        c_country_id: new EntityColumn<string>({ name: 'c_country_id', type: 'char', length: 3, flags: c.PK, prompt: 'Country' }),
        c_province_id: new EntityColumn<string>({ name: 'c_province_id', type: 'char', length: 3, flags: c.PK, prompt: 'Province' }),
        vc_province: new EntityColumn<string>({ name: 'vc_province', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Name' }),
        ...DefaultColumns()
    };
    views = {
        prv_brwStandard: {
            name: 'prv_brwStandard',
            keyMappings: { c_country_id: 0, c_province_id: 1 },
            compoundKeyGroups: {
                CountryProvince: { viewIndex: 2, keyMappings: { c_country_id: 0, c_province_id: 1 }, keySeparator: '-' },
            },
        },
    };
    constructor() { super('Provinces'); }
}

export class Provinces extends Entity<ProvincesDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new ProvincesDef(), parentKeys);
        this.Title = "Provinces";
        this.Icon = IconMap;
        this.Form = "AutoForm";
    }
}

// ---- Customers ----

export class CustomerFiltersDef extends EntityDefinition {
    columns = {
        c_category_filter: new EntityColumn<string>({ name: 'c_category_filter', type: 'char', length: 20, flags: c.Edit | f.nullable, prompt: 'Category code', placeholder: 'e.g. RETAIL' }),
        c_country_filter: new EntityColumn<string>({ name: 'c_country_filter', type: 'char', length: 3, flags: c.Edit | f.nullable, prompt: 'Country code', placeholder: 'e.g. AR' }),
    };
    constructor() { super('CustomerFilters'); }
}

export class CustomerFilters extends Entity<CustomerFiltersDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new CustomerFiltersDef(), parentKeys);
        this.Title = "Customer filters";
        this.Icon = IconFilter;
        this.Form = "AutoFiltersForm";
    }
}

const customerActions = (): Record<string, EntityClientAction> => ({
    approve: {
        name: 'approve',
        title: 'Approve',
        label: 'Approve',
        icon: <IconCheck size="1rem" />,
        refreshOnClose: true,
        minSelectedRecords: 1,
        onClick: async ({ entity, selectedKeys, onClose }) => {
            const result = await entity.API.executeProcess({ name: 'cus_approve' }, undefined, null, selectedKeys);
            await onClose?.(!result.Failed);
            return true;
        },
    },
    sendNewsletter: {
        name: 'sendNewsletter',
        title: 'Send newsletter',
        label: 'Newsletter',
        icon: <IconMail size="1rem" />,
        dontRequireSelection: true,
        onClick: async ({ modal, onClose }) => {
            await modal?.open({
                modalProps: { title: <Text fw={700}>Newsletter</Text> },
                content: <Stack><NotifySuccess title="Queued">The newsletter was queued for every active customer (mock).</NotifySuccess></Stack>,
            });
            await onClose?.(true);
            return true;
        },
    },
});

const customersColumns = () => ({
    c_customer_id: new EntityColumn<string>({ name: 'c_customer_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'Customer' }),
    vc_name: new EntityColumn<string>({ name: 'vc_name', type: 'varchar', length: 100, flags: c.Edit, prompt: 'Full name' }),
    vc_email: new EntityColumn<string>({ name: 'vc_email', type: 'varchar', length: 255, flags: c.Edit | f.nullable, prompt: 'Email' }),
    vc_phone: new EntityColumn<string>({ name: 'vc_phone', type: 'varchar', length: 50, flags: c.Edit | f.nullable, prompt: 'Phone' }),
    c_category_id: new EntityColumn<string>({ name: 'c_category_id', type: 'char', length: 20, flags: c.Edit, prompt: 'Category' }),
    c_country_id: new EntityColumn<string>({ name: 'c_country_id', type: 'char', length: 3, flags: c.Edit | f.nullable, prompt: 'Country' }),
    c_province_id: new EntityColumn<string>({ name: 'c_province_id', type: 'char', length: 3, flags: c.Edit | f.nullable, prompt: 'Province' }),
    vc_tags: new EntityColumn<string[]>({ name: 'vc_tags', type: 'varchar', flags: c.Edit | f.nullable, prompt: 'Tags', isArray: true }),
    dt_since: new EntityColumn<Date>({ name: 'dt_since', type: 'date', flags: c.Edit, prompt: 'Customer since' }),
    m_credit_limit: new EntityColumn<number>({ name: 'm_credit_limit', type: 'money', flags: c.Edit | f.nullable, prompt: 'Credit limit' }),
    bt_active: new EntityColumn<boolean>({ name: 'bt_active', type: 'bit', flags: c.Edit, prompt: 'Active' }),
    m_latitude: new EntityColumn<number>({ name: 'm_latitude', type: 'decimal', scale: 6, flags: c.Edit | f.nullable, prompt: 'Latitude', excludeInAutoForm: true }),
    m_longitude: new EntityColumn<number>({ name: 'm_longitude', type: 'decimal', scale: 6, flags: c.Edit | f.nullable, prompt: 'Longitude', excludeInAutoForm: true }),
    vc_notes: new EntityColumn<string>({ name: 'vc_notes', type: 'varchar', length: 1000, flags: c.Edit | f.nullable, prompt: 'Notes' }),
    ...DefaultColumns()
});

export class CustomersDef extends EntityDefinition {
    columns = customersColumns();
    views = {
        cus_brwMap: {
            name: 'cus_brwMap',
            keyMappings: { c_customer_id: 0 },
            mapInfoWindowRenderer: (record: unknown[]) => <Stack spacing={2}><Text fw={600}>{String(record[1])}</Text><Text size="xs">{String(record[4] ?? '')}</Text></Stack>,
        },
        cus_brwStandard: {
            name: 'cus_brwStandard',
            keyMappings: { c_customer_id: 0 },
            FiltersEntity: (client: MicroMClient, parentKeys?: ValuesObject) => new CustomerFilters(client, parentKeys),
        },
    };
    lookups = {
        Categories: lookup('Categories', (cl, pk) => new Categories(cl, pk)),
        Countries: lookup('Countries', (cl, pk) => new Countries(cl, pk)),
        Provinces: lookup('Provinces', (cl, pk) => new Provinces(cl, pk), { view: 'prv_brwStandard', compoundKeyGroupName: 'CountryProvince' }),
        Tags: lookup('Tags', (cl, pk) => new Tags(cl, pk)),
    };
    clientActions = customerActions();
    procs = { cus_approve: { name: 'cus_approve' } };
    constructor() { super('Customers'); }
}

export class Customers extends Entity<CustomersDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new CustomersDef(), parentKeys);
        this.Title = "Customers";
        this.Icon = IconAddressBook;
        this.HelpText = "Sample entity served by MockMicroMClient.";
        this.Form = import('./CustomersForm').then(m => m.CustomersForm);
    }
}

export class CustomersAutoForm extends Entity<CustomersDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new CustomersDef(), parentKeys);
        this.Title = "Customers (AutoForm)";
        this.Icon = IconAddressBook;
        this.Form = "AutoForm";
    }
}

// ---- Branches (second MultiDataMap layer) ----

export class BranchesDef extends EntityDefinition {
    columns = {
        c_branch_id: new EntityColumn<string>({ name: 'c_branch_id', type: 'char', length: 20, flags: c.PK, prompt: 'Branch' }),
        vc_name: new EntityColumn<string>({ name: 'vc_name', type: 'varchar', length: 100, flags: c.Edit, prompt: 'Name' }),
        vc_address: new EntityColumn<string>({ name: 'vc_address', type: 'varchar', length: 255, flags: c.Edit | f.nullable, prompt: 'Address' }),
        m_latitude: new EntityColumn<number>({ name: 'm_latitude', type: 'decimal', scale: 6, flags: c.Edit, prompt: 'Latitude' }),
        m_longitude: new EntityColumn<number>({ name: 'm_longitude', type: 'decimal', scale: 6, flags: c.Edit, prompt: 'Longitude' }),
        ...DefaultColumns()
    };
    views = { bra_brwStandard: { name: 'bra_brwStandard', keyMappings: { c_branch_id: 0 } } };
    constructor() { super('Branches'); }
}

export class Branches extends Entity<BranchesDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new BranchesDef(), parentKeys);
        this.Title = "Branches";
        this.Icon = IconBuildingStore;
        this.Form = "AutoForm";
    }
}

// ---- Documents (FileUploader / AvatarUploader) ----

export class DocumentsDef extends EntityDefinition {
    columns = {
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: f.pk, prompt: 'File process', defaultValue: '' }),
        vc_fileguid: new EntityColumn<string>({ name: 'vc_fileguid', type: 'varchar', length: 80, flags: f.None, prompt: 'File GUID', defaultValue: '' }),
        ...DefaultColumns()
    };
    constructor() { super('Documents'); }
}

export class Documents extends Entity<DocumentsDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new DocumentsDef(), parentKeys);
        this.Title = "Documents";
        this.Icon = IconFile;
    }
}

// ---- FieldsDemo / LookupsDemo ----

const fieldsDemoColumns = () => ({
    c_demo_id: new EntityColumn<string>({ name: 'c_demo_id', type: 'char', length: 20, flags: c.PK, prompt: 'Code', description: 'Primary key (char 20)' }),
    vc_text: new EntityColumn<string>({ name: 'vc_text', type: 'varchar', length: 100, flags: c.Edit, prompt: 'Text', description: 'TextField', placeholder: 'Type something...' }),
    vc_email: new EntityColumn<string>({ name: 'vc_email', type: 'varchar', length: 255, flags: c.Edit | f.nullable, prompt: 'Email', description: 'EmailField' }),
    vc_phone: new EntityColumn<string>({ name: 'vc_phone', type: 'varchar', length: 50, flags: c.Edit | f.nullable, prompt: 'Phone', description: 'PhoneField' }),
    vc_url: new EntityColumn<string>({ name: 'vc_url', type: 'varchar', length: 255, flags: c.Edit | f.nullable, prompt: 'Website', description: 'UrlField' }),
    vc_cuit: new EntityColumn<string>({ name: 'vc_cuit', type: 'varchar', length: 13, flags: c.Edit | f.nullable, prompt: 'CUIT', description: 'CUITField' }),
    vc_password: new EntityColumn<string>({ name: 'vc_password', type: 'varchar', length: 255, flags: c.Edit | f.nullable, prompt: 'Password', description: 'PasswordField' }),
    i_quantity: new EntityColumn<number>({ name: 'i_quantity', type: 'int', flags: c.Edit, prompt: 'Quantity', description: 'NumberField (int)' }),
    m_amount: new EntityColumn<number>({ name: 'm_amount', type: 'money', flags: c.Edit | f.nullable, prompt: 'Amount', description: 'NumberField (money)' }),
    m_budget: new EntityColumn<number>({ name: 'm_budget', type: 'money', flags: c.Edit | f.nullable, prompt: 'Budget', description: 'MoneyRangeField' }),
    bi_quota: new EntityColumn<number>({ name: 'bi_quota', type: 'bigint', flags: c.Edit | f.nullable, prompt: 'Storage quota', description: 'BytesRangeField' }),
    dt_date: new EntityColumn<Date>({ name: 'dt_date', type: 'date', flags: c.Edit | f.nullable, prompt: 'Date', description: 'DateInputField' }),
    t_time: new EntityColumn<string>({ name: 't_time', type: 'time', flags: c.Edit | f.nullable, prompt: 'Time', description: 'TimeField' }),
    dt_week_start: new EntityColumn<Date>({ name: 'dt_week_start', type: 'date', flags: c.Edit | f.nullable, prompt: 'Week', description: 'WeekPickerField' }),
    dt_week_end: new EntityColumn<Date>({ name: 'dt_week_end', type: 'date', flags: c.Edit | f.nullable, prompt: 'Week end' }),
    bt_terms: new EntityColumn<boolean>({ name: 'bt_terms', type: 'bit', flags: c.Edit, prompt: 'I accept the terms', description: 'CheckboxField' }),
    bt_notifications: new EntityColumn<boolean>({ name: 'bt_notifications', type: 'bit', flags: c.Edit, prompt: 'Notifications', description: 'SwitchField' }),
    c_priority: new EntityColumn<string>({ name: 'c_priority', type: 'char', length: 20, flags: c.Edit | f.nullable, prompt: 'Priority', description: 'RadioGroupField' }),
    c_category_id: new EntityColumn<string>({ name: 'c_category_id', type: 'char', length: 20, flags: c.Edit | f.nullable, prompt: 'Category', description: 'Lookup' }),
    c_category2_id: new EntityColumn<string>({ name: 'c_category2_id', type: 'char', length: 20, flags: c.Edit | f.nullable, prompt: 'Category (select)', description: 'LookupSelect' }),
    vc_tags: new EntityColumn<string[]>({ name: 'vc_tags', type: 'varchar', flags: c.Edit | f.nullable, prompt: 'Tags', description: 'LookupMultiSelect', isArray: true }),
    c_country_id: new EntityColumn<string>({ name: 'c_country_id', type: 'char', length: 3, flags: c.Edit | f.nullable, prompt: 'Country', description: 'Lookup' }),
    c_province_id: new EntityColumn<string>({ name: 'c_province_id', type: 'char', length: 3, flags: c.Edit | f.nullable, prompt: 'Province', description: 'Lookup with bindingColumns (CompoundLookup)' }),
    vc_pin: new EntityColumn<string>({ name: 'vc_pin', type: 'varchar', length: 6, flags: c.Edit | f.nullable, prompt: 'Verification code', description: 'PinField' }),
    vc_notes: new EntityColumn<string>({ name: 'vc_notes', type: 'varchar', length: 2000, flags: c.Edit | f.nullable, prompt: 'Notes', description: 'TextAreaField' }),
    ...DefaultColumns()
});

export class FieldsDemoDef extends EntityDefinition {
    columns = fieldsDemoColumns();
    views = { fld_brwStandard: { name: 'fld_brwStandard', keyMappings: { c_demo_id: 0 } } };
    lookups = {
        Categories: lookup('Categories', (cl, pk) => new Categories(cl, pk)),
        Countries: lookup('Countries', (cl, pk) => new Countries(cl, pk)),
        Provinces: lookup('Provinces', (cl, pk) => new Provinces(cl, pk), { view: 'prv_brwStandard', compoundKeyGroupName: 'CountryProvince' }),
        Tags: lookup('Tags', (cl, pk) => new Tags(cl, pk)),
    };
    constructor() { super('FieldsDemo'); }
}

export class FieldsDemo extends Entity<FieldsDemoDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new FieldsDemoDef(), parentKeys);
        this.Title = "Fields demo";
        this.Icon = IconForms;
        this.Form = "AutoForm";
    }
}
