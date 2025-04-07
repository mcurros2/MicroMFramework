export const RECORDED_ACCESS_DATA_STORAGE_KEY = 'mm_menu_paths';

export enum AllowedRouteFlags {
    None = 0,
    Insert = 1,
    Update = 2,
    Delete = 4,
    Get = 8,
    DefaultLookup = 16,
    Edit = Insert | Update | Delete | Get,
    CustomLookup = 32,
    Views = 64,
    Procs = 128,
    Actions = 256,
    Import = 512,
    All = 1023
}

export interface RecordedAccessData {
    entityName: string,
    access: AllowedRouteFlags,
    views?: string[],
    procs?: string[],
    actions?: string[],
    lookups?: string[],
}



const CsharpAllowedRouteFlags: { name: string; value: number }[] = [
    { name: "All", value: 511 },
    { name: "Actions", value: 256 },
    { name: "Procs", value: 128 },
    { name: "Views", value: 64 },
    { name: "CustomLookup", value: 32 },
    { name: "DefaultLookup", value: 16 },
    { name: "Get", value: 8 },
    { name: "Delete", value: 4 },
    { name: "Update", value: 2 },
    { name: "Insert", value: 1 },
    { name: "None", value: 0 },
];

function mapAccessToFlags(access: number): string {
    if (access === 0) return "AllowedRouteFlags.None";
    const flags: string[] = [];

    if ((access & AllowedRouteFlags.Edit) === AllowedRouteFlags.Edit) {
        flags.push("AllowedRouteFlags.Edit");
        access &= ~AllowedRouteFlags.Edit;
    }

    CsharpAllowedRouteFlags.forEach(flag => {
        if (flag.name === "None") return;
        if ((access & flag.value) === flag.value) {
            flags.push(`AllowedRouteFlags.${flag.name}`);
        }
    });

    return flags.join(" | ");
}

export function generateCSharpGetRoutePaths(dataRecord: Record<string, RecordedAccessData>): string {
    const csharpLines: string[] = [];

    Object.values(dataRecord).forEach(data => {
        const { entityName, access, views, procs, actions, lookups } = data;

        let flags = mapAccessToFlags(access);

        const combinedProcs = [...new Set([...(procs || []), ...(lookups || [])])];

        if (lookups && lookups.length > 0) {
            if (!flags.includes("AllowedRouteFlags.CustomLookup")) {
                flags = flags ? `${flags} | AllowedRouteFlags.CustomLookup` : "AllowedRouteFlags.CustomLookup";
            }
            if (!flags.includes("AllowedRouteFlags.Procs")) {
                flags = flags ? `${flags} | AllowedRouteFlags.Procs` : "AllowedRouteFlags.Procs";
            }
        }

        const params: string[] = [`${flags}`];

        if (views && views.length > 0) {
            const viewsParam = `[${views.map(v => `"${v}"`).join(", ")}]`;
            params.push(`views: ${viewsParam}`);
        }

        if (combinedProcs.length > 0) {
            const procsParam = `[${combinedProcs.map(p => `"${p}"`).join(", ")}]`;
            params.push(`procs: ${procsParam}`);
        }

        if (actions && actions.length > 0) {
            const actionsParam = `[${actions.map(a => `"${a}"`).join(", ")}]`;
            params.push(`actions: ${actionsParam}`);
        }

        const csharpLine = `..typeof(${entityName}).GetRoutePaths(${params.join(", ")})`;

        csharpLines.push(csharpLine + ",");
    });

    return csharpLines.join("\n");
}

