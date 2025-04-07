export type Value = string | number | Date | boolean | string[] | null;
export type ValuesObject = Record<string, Value>;
export type ValuesRecord = Value[];

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

export function isDBStatusResult(data: any): data is DBStatusResult {
    return data
        && typeof data.Failed === "boolean"
        && typeof data.AutonumReturned === "boolean"
}
