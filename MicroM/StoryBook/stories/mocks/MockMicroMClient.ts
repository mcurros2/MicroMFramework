import {
    DataResult, DBStatusResult, Entity, EntityDefinition, FileUploadResponse, ImpDataResult, MicroMClient, MicroMClientClaimTypes, MicroMToken,
    SQLType, TotpAuthenticatorsResponse, TotpSetupStartResponse, TwoFactorLoginResult, Value, ValuesObject
} from "@mcurros2/microm";

export interface MockTable {
    entityName: string,
    pk: string[],
    autonumColumn?: string,
    descriptionColumn: string,
    viewColumns: MockViewColumn[],
    // Extra views by name; any other view name falls back to viewColumns
    views?: Record<string, MockViewColumn[]>,
    // Equality filters applied when the request carries a non-empty value (parent keys, filter forms)
    filterBy?: (string | { param: string, column: string })[],
    rows: ValuesObject[],
}

export interface MockViewColumn {
    name: string,
    header: string,
    type: SQLType,
    // View-only computed value (e.g. FK description); not returned by GET.
    compute?: (row: ValuesObject) => Value,
}

export type MockTwoFactorFlow = NonNullable<TwoFactorLoginResult["two_factor_flow"]>;

export interface MockMicroMClientOptions {
    latency?: number,
    logRequests?: boolean,
    loggedIn?: boolean,
    twoFactorFlow?: MockTwoFactorFlow,
}

const OK: DBStatusResult = { Failed: false, AutonumReturned: false, Results: [{ Status: 0, Message: "OK" }] };

const QR_PLACEHOLDER = "data:image/svg+xml;utf8," + encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160"><rect width="160" height="160" fill="#fff"/>` +
    `<rect x="10" y="10" width="40" height="40" fill="#000"/><rect x="110" y="10" width="40" height="40" fill="#000"/>` +
    `<rect x="10" y="110" width="40" height="40" fill="#000"/><text x="80" y="88" font-size="16" text-anchor="middle">MOCK QR</text></svg>`);

function wait(ms: number, signal: AbortSignal | null) {
    return new Promise<void>((resolve, reject) => {
        if (signal?.aborted) { reject(new DOMException("Aborted", "AbortError")); return; }
        const t = setTimeout(resolve, ms);
        signal?.addEventListener("abort", () => { clearTimeout(t); reject(new DOMException("Aborted", "AbortError")); }, { once: true });
    });
}

function normalize(v: Value | undefined): string {
    if (v === null || v === undefined) return "";
    if (v instanceof Date) return v.toISOString();
    if (Array.isArray(v)) return v.join(",");
    return String(v);
}

function stripAccents(s: string) {
    return s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
}

function newGuid() {
    return typeof crypto !== "undefined" && "randomUUID" in crypto
        ? crypto.randomUUID()
        : `${Date.now().toString(16)}-${Math.random().toString(16).slice(2)}`;
}

// Extends MicroMClient so EntityAPI's instanceof check passes. Never hits the network.
export class MockMicroMClient extends MicroMClient {
    #tables = new Map<string, MockTable>();
    #files = new Map<string, { file: Blob, url: string }>();
    #authenticators = [{ authenticator_id: "1", authenticator_name: "Phone" }];
    #latency: number;
    #log: boolean;
    #logged: boolean;
    #twoFactorFlow?: MockTwoFactorFlow;
    #claims: Partial<MicroMClientClaimTypes> = { username: "demo", useremail: "demo@example.com", userinitials: "DM" };

    constructor(options?: MockMicroMClientOptions) {
        super({ api_url: "https://mock.microm.local", app_id: "storybook" });
        this.#latency = options?.latency ?? 350;
        this.#log = options?.logRequests ?? true;
        this.#logged = options?.loggedIn ?? true;
        this.#twoFactorFlow = options?.twoFactorFlow;
    }

    registerTable(table: MockTable) {
        this.#tables.set(table.entityName, table);
        return this;
    }

    table(entityName: string) {
        return this.#tables.get(entityName);
    }

    static viewColumnsFrom(entity: Entity<EntityDefinition>, columns: string[]): MockViewColumn[] {
        return columns.map(name => {
            const col = entity.def.columns[name];
            if (!col) throw new Error(`Column ${name} not found in ${entity.name}`);
            return { name, header: col.prompt || name, type: col.type as SQLType };
        });
    }

    #table(entity_name: string) {
        const t = this.#tables.get(entity_name);
        if (!t) throw { status: 404, statusMessage: `MockMicroMClient: entity ${entity_name} not registered`, url: entity_name };
        return t;
    }

    #findIndex(t: MockTable, values: ValuesObject | null) {
        if (!values) return -1;
        return t.rows.findIndex(r => t.pk.every(k => normalize(r[k]).trim() === normalize(values[k]).trim()));
    }

    #trace(action: string, entity: string, extra?: unknown) {
        if (this.#log) console.debug(`[MockMicroMClient] ${action} ${entity}`, extra ?? "");
    }

    #token(username: string) {
        this.#claims = { ...this.#claims, username };
        return new MicroMToken("mock-access-token", 3600, "mock-refresh-token", "Bearer", this.#claims);
    }

    override get LOGGED_IN_USER() { return this.#logged ? this.#claims : undefined; }
    override getMenus() { return new Set<string>(); }
    override async isLoggedIn() { return this.#logged; }
    override async isLoggedInLocal() { return this.#logged; }
    override async getRememberUser() { return "demo"; }

    override async login(username: string): Promise<MicroMToken | TwoFactorLoginResult> {
        await wait(this.#latency, null);
        this.#trace("login", username);
        if (this.#twoFactorFlow) {
            return {
                requires_two_factor: true, two_factor_challenge_id: "mock-challenge", two_factor_flow: this.#twoFactorFlow, username,
                email: "demo@example.com", qr_code_data_url: this.#twoFactorFlow === "sql_admin_setup" ? QR_PLACEHOLDER : undefined,
            };
        }
        this.#logged = true;
        return this.#token(username);
    }

    override async login2fa(_challengeId: string, code: string, _rememberme?: boolean, username?: string): Promise<MicroMToken> {
        await wait(this.#latency, null);
        this.#trace("login2fa", code);
        if (code !== "123456") throw { status: 401, statusMessage: "Invalid code (use 123456)", message: "Invalid code (use 123456)" };
        this.#logged = true;
        return this.#token(username ?? "demo");
    }

    override async sendTwoFactorEmailCode() { await wait(this.#latency, null); }
    override async logoff() { this.#logged = false; }
    override async localLogoff() { this.#logged = false; }
    override async recoveryemail(): Promise<DBStatusResult> { await wait(this.#latency, null); return OK; }
    override async recoverpassword(): Promise<DBStatusResult> { await wait(this.#latency, null); return OK; }

    override async startTotpSetup(authenticatorName: string): Promise<TotpSetupStartResponse> {
        await wait(this.#latency, null);
        this.#trace("startTotpSetup", authenticatorName);
        return { setup_challenge_id: `setup-${authenticatorName}`, qr_code_data_url: QR_PLACEHOLDER };
    }

    override async confirmTotpSetup(code: string, setupChallengeId: string) {
        await wait(this.#latency, null);
        if (code !== "123456") throw { status: 400, statusMessage: "Invalid code (use 123456)", message: "Invalid code (use 123456)" };
        this.#authenticators.push({ authenticator_id: String(Date.now()), authenticator_name: setupChallengeId.replace(/^setup-/, "") });
    }

    override async listTotpAuthenticators(): Promise<TotpAuthenticatorsResponse> {
        await wait(this.#latency, null);
        return { authenticators: [...this.#authenticators] };
    }

    override async deleteTotpAuthenticator(authenticatorId: string) {
        await wait(this.#latency, null);
        this.#authenticators = this.#authenticators.filter(a => a.authenticator_id !== authenticatorId);
    }

    override getDocumentURL(fileGuid: string) { return fileGuid ? (this.#files.get(fileGuid)?.url ?? "") : ""; }
    override getThumbnailURL(fileGuid: string) { return this.getDocumentURL(fileGuid); }

    override async downloadBlob(fileUrl: string): Promise<Blob> {
        for (const f of this.#files.values()) if (f.url === fileUrl) return f.file;
        return (await fetch(fileUrl)).blob();
    }

    // Uploaded files live as object URLs and are listed through the FileStoreClient table when it is registered.
    override async upload(file: File, fileprocess_id: string, abort_signal: AbortSignal | null = null, _max = 150, _q = 75, onProgress: (file: File, progress: number) => void = () => { }): Promise<FileUploadResponse> {
        for (const p of [20, 45, 70, 100]) {
            await wait(Math.round(this.#latency / 3), abort_signal);
            onProgress(file, p);
        }
        const guid = newGuid();
        const url = URL.createObjectURL(file);
        this.#files.set(guid, { file, url });
        this.#tables.get("FileStoreClient")?.rows.push({
            vc_fileguid: guid, c_fileprocess_id: fileprocess_id, vc_filename: file.name, bi_filesize: file.size, c_fileuploadstatus_id: "Uploaded",
        });
        this.#trace("upload", file.name, { guid, fileprocess_id });
        return { vc_fileguid: guid, FileProcessId: fileprocess_id, documentURL: url, thumbnailURL: url } as FileUploadResponse;
    }

    override async get(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, abort_signal: AbortSignal | null = null): Promise<ValuesObject> {
        await wait(this.#latency, abort_signal);
        this.#trace("get", entity_name, values);
        const t = this.#table(entity_name);
        const idx = this.#findIndex(t, values);
        return idx >= 0 ? { ...t.rows[idx] } : null!;
    }

    override async insert(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, _rs: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        await wait(this.#latency, abort_signal);
        this.#trace("insert", entity_name, values);
        const t = this.#table(entity_name);
        const row: ValuesObject = { ...values };
        if (t.autonumColumn) {
            const next = t.rows.reduce((max, r) => Math.max(max, parseInt(normalize(r[t.autonumColumn!]), 10) || 0), 0) + 1;
            row[t.autonumColumn] = String(next);
            t.rows.push(row);
            return { Failed: false, AutonumReturned: true, Results: [{ Status: 15, Message: String(next) }] };
        }
        if (this.#findIndex(t, row) >= 0) {
            return { Failed: true, AutonumReturned: false, Results: [{ Status: 4, Message: "Record already exists" }] };
        }
        t.rows.push(row);
        return OK;
    }

    override async update(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, _rs: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        await wait(this.#latency, abort_signal);
        this.#trace("update", entity_name, values);
        const t = this.#table(entity_name);
        const idx = this.#findIndex(t, values);
        if (idx < 0) return { Failed: true, AutonumReturned: false, Results: [{ Status: 11, Message: "Record not found" }] };
        t.rows[idx] = { ...t.rows[idx], ...values };
        return OK;
    }

    override async delete(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject | null, recordsSelection: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        await wait(this.#latency, abort_signal);
        this.#trace("delete", entity_name, recordsSelection?.length ? recordsSelection : values);
        const t = this.#table(entity_name);
        const targets = recordsSelection?.length ? recordsSelection : (values ? [values] : []);
        for (const keys of targets) {
            const idx = this.#findIndex(t, keys);
            if (idx >= 0) t.rows.splice(idx, 1);
        }
        return OK;
    }

    override async lookup(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, _lookup_name: string | null = null, abort_signal: AbortSignal | null = null) {
        await wait(Math.round(this.#latency / 2), abort_signal);
        this.#trace("lookup", entity_name, values);
        const t = this.#table(entity_name);
        const idx = this.#findIndex(t, values);
        return { Description: idx >= 0 ? normalize(t.rows[idx][t.descriptionColumn]) : "" };
    }

    #rows(t: MockTable, cols: MockViewColumn[], values: ValuesObject | null) {
        const like = (Array.isArray(values?.like) ? values!.like : []) as string[];
        const terms = like.map(s => stripAccents(s.replace(/%/g, "").trim())).filter(Boolean);
        const limit = parseInt(normalize(values?.["@row_limit"] ?? ""), 10);
        const cell = (r: ValuesObject, c: MockViewColumn) => (c.compute ? c.compute(r) : r[c.name]) ?? null;

        let rows = t.rows;
        for (const f of t.filterBy ?? []) {
            const { param, column } = typeof f === "string" ? { param: f, column: f } : f;
            const v = normalize(values?.[param]);
            if (v) rows = rows.filter(r => normalize(r[column]) === v);
        }
        if (terms.length) {
            rows = rows.filter(r => terms.every(term => cols.some(c => stripAccents(normalize(cell(r, c))).includes(term))));
        }
        if (limit > 0) rows = rows.slice(0, limit);
        return rows.map(r => cols.map(c => cell(r, c)));
    }

    override async view(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, view_name: string, abort_signal: AbortSignal | null = null): Promise<DataResult[]> {
        await wait(this.#latency, abort_signal);
        this.#trace("view", `${entity_name}/${view_name}`, values);
        const t = this.#table(entity_name);
        const cols = t.views?.[view_name] ?? t.viewColumns;
        return [{
            Header: cols.map(c => c.header),
            typeInfo: cols.map(c => c.type),
            records: this.#rows(t, cols, values),
        }];
    }

    override async proc(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, _rs: ValuesObject[] | null, proc_name: string, abort_signal: AbortSignal | null = null): Promise<DataResult[]> {
        return this.view(entity_name, parent_keys, values, proc_name, abort_signal);
    }

    override async process(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, rs: ValuesObject[] | null, proc_name: string, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        await wait(this.#latency, abort_signal);
        this.#trace("process", `${entity_name}/${proc_name}`, { values, rs });
        return OK;
    }

    override async action<TReturn>(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, action_name: string, abort_signal: AbortSignal | null = null): Promise<TReturn> {
        await wait(this.#latency, abort_signal);
        this.#trace("action", `${entity_name}/${action_name}`, values);
        return {} as TReturn;
    }

    override async import(entity_name: string, _parent_keys: ValuesObject | null, values: ValuesObject, import_procname: string | null, abort_signal: AbortSignal | null = null): Promise<ImpDataResult> {
        await wait(this.#latency * 3, abort_signal);
        this.#trace("import", entity_name, { values, import_procname });
        this.#tables.get("ImportProcess")?.rows.push({
            c_import_process_id: String(Date.now()), c_fileprocess_id: values.c_fileprocess_id, i_total_records: 3, i_errors: 1,
            vc_assemblytypename: entity_name, vc_import_procname: import_procname, c_import_status_id: "Completed", vc_fileguid: "",
        });
        return { ProcessedCount: 3, SuccessCount: 2, ErrorCount: 1, Errors: { 3: "Row 3: invalid value (mock)" } };
    }

    override async viewtoexcel(): Promise<Blob> {
        return new Blob(["Storybook mock export"], { type: "text/plain" });
    }

    override async proctoexcel(): Promise<Blob> {
        return new Blob(["Storybook mock export"], { type: "text/plain" });
    }
}
