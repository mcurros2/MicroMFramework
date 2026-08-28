export type Value = string | number | Date | boolean | string[] | null;
export type ValuesObject = Record<string, Value>;
export type ValuesRecord = Value[];

export interface MicroMRequestOptions {
    keepalive?: boolean;
}

export interface FileImportColumnMapping {
    SourceHeader: string | null,
    SourceIndex: number | null,
    DestinationColumnName: string,
}

export interface FileImportMapping {
    SheetName: string | null,
    Mapping: FileImportColumnMapping[],
}

export type SQLType = 'char' | 'nchar' | 'varchar' | 'nvarchar' | 'text' | 'ntext' | 'tinyint' | 'smallint' | 'int' | 'bigint' | 'float' | 'decimal' | 'real' | 'bit' | 'money' | 'datetime2' | 'datetime' | 'smalldatetime' | 'date' | 'binary' | 'varbinary' | 'image' | 'time';

export interface DataResult {
    Header: string[],
    typeInfo: SQLType[],
    records: ValuesRecord[]
}

export interface DBStatus {
    Status: number;
    Message: string;
}

export enum DBStatusCodes {
    "OK" = 0,
    "AUTONUM" = 15,
    "RECORD_CHANGED" = 4,
    "ERROR" = 11
};

export interface DBStatusResult {
    Failed: boolean;
    AutonumReturned: boolean;
    Results: DBStatus[];
}

export function isDBStatusResult(data: unknown): data is DBStatusResult {
    if (!data || typeof data !== "object") return false;

    const result = data as Partial<DBStatusResult>;
    return typeof result.Failed === "boolean"
        && typeof result.AutonumReturned === "boolean"
}
