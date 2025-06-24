import { SQLType, Value } from "../../client";


export function formatDate(value: Value, locale?: string, dateOptions?: Intl.DateTimeFormatOptions): string {
    const date = new Date(String(value));
    return new Intl.DateTimeFormat(locale, dateOptions).format(date);
}

export function formatTime(value: Value, locale?: string, dateOptions?: Intl.DateTimeFormatOptions): string {
    const timeString = String(value);
    const [hours, minutes, seconds] = timeString.split(':');
    const [mainSeconds, milliseconds] = seconds?.split('.') || [];

    const date = new Date(2000, 0, 1, Number(hours), Number(minutes), Number(mainSeconds));

    return new Intl.DateTimeFormat(locale, dateOptions).format(date);
}

export function formatNumber(value: Value, locale?: string): string {
    return new Intl.NumberFormat(locale).format(Number(value));
}

// fix to handle dates without timezone
export function convertDateToNative(value: string, serverTimeZoneOffset: number): Date {
    // If the value already includes an explicit timezone indicator, parse as is.
    if (value.endsWith('Z') || value.includes('GMT') || value.includes('GMT+') || value.includes('GMT-')) {
        return new Date(value);
    }

    if (serverTimeZoneOffset === undefined) serverTimeZoneOffset = 0;

    // Otherwise, add server timezone offset to the date string.
    const date = new Date(value);
    const localTimeZoneOffset = date.getTimezoneOffset() * 60000; // Convert minutes to milliseconds
    const adjustedTime = date.getTime() + localTimeZoneOffset + (serverTimeZoneOffset * 3600000); // Convert hours to milliseconds
    return new Date(adjustedTime);
}

export function formatSQLValue(value: Value, sqlType: SQLType, locale?: string): string {
    if (value === null) {
        return 'null';
    }
    switch (sqlType) {
        case 'date':
            return formatDate(value, locale, { day: "2-digit", month: "2-digit", year: "numeric" });
        case 'datetime':
        case 'datetime2':
            return formatDate(value, locale, { day: "2-digit", month: "2-digit", year: "numeric", hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone });
        case 'smalldatetime':
            return formatDate(value, locale, { day: "2-digit", month: "2-digit", year: "numeric", hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit" });
        case 'time':
            return formatTime(value, locale, { hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit" });
        case 'float':
        case 'decimal':
        case 'real':
        case 'money':
            return formatNumber(value, locale);
        // For text-based SQL types and others, we'll return the value as a string for now.
        case 'char':
        case 'nchar':
        case 'varchar':
        case 'nvarchar':
        case 'text':
        case 'ntext':
        case 'int':
        case 'bigint':
        case 'bit':
        case 'binary':
        case 'varbinary':
        case 'image':
        default:
            return String(value);
    }
}

export function convertToNativeValue(value: Value, sqlType: SQLType, serverTimezoneOffset: number): Value {
    if (typeof value !== 'string') {
        return value;
    }

    switch (sqlType) {
        case 'char':
        case 'nchar':
        case 'varchar':
        case 'nvarchar':
        case 'text':
        case 'ntext':
        case 'binary':
        case 'varbinary':
        case 'image':
        case 'time': // Time is returned as a string as-is
            return value;

        case 'smallint':
        case 'int':
        case 'bigint':
        case 'float':
        case 'real':
        case 'decimal':
        case 'money':
            return Number(value);

        case 'datetime2':
        case 'datetime':
        case 'smalldatetime':
        case 'date':
            return convertDateToNative(value, serverTimezoneOffset);

        default:
            return value;
    }
}
