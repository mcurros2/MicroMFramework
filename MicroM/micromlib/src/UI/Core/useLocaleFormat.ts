import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useState } from "react";
import { EntityColumn } from "../../Entity";
import { SQLType, Value } from "../../client";
import { convertToNativeValue, formatSQLValue } from "./formatting";

export interface UseLocaleFormatProps {
    timeZoneOffset: number,
    initialLocale?: string
}

export const UseLocaleFormatDefaultProps: Partial<UseLocaleFormatProps> = {
    initialLocale: navigator.language
}

export function useLocaleFormat(props: UseLocaleFormatProps) {
    const { initialLocale, timeZoneOffset } = useComponentDefaultProps('useLocaleFormat', UseLocaleFormatDefaultProps, props);

    const [locale, setLocale] = useState(initialLocale);

    const formatValue = useCallback(function (value: Value, sqlType: SQLType) {
        const result = formatSQLValue(value, sqlType, locale);
        return result;
    }, [locale, timeZoneOffset]);

    const getNativeValue = useCallback(function (value: Value, sqlType: SQLType) {
        const result = convertToNativeValue(value, sqlType, timeZoneOffset);
        return result;
    }, [locale, timeZoneOffset]); // locale is here so the values captured here are in sync with formatValue

    const formatColumnValue = useCallback((col: EntityColumn<Value>) => {
        const result = formatSQLValue(col.value, col.type, locale)
        return result === 'null' ? '' : result;
    }, [locale, timeZoneOffset]);

    return {
        locale,
        setLocale,
        formatValue,
        getNativeValue,
        formatColumnValue
    };
}