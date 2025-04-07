import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useState } from "react";
import { EntityColumn } from "../../Entity";
import { SQLType, Value } from "../../client";
import { convertToNativeType, formatSQLValue } from "./formatting";

export interface UseLocaleFormatProps {
    initialLocale?: string
}

export const UseLocaleFormatDefaultProps: UseLocaleFormatProps = {
    initialLocale: navigator.language
}

export function useLocaleFormat(props: UseLocaleFormatProps) {
    const { initialLocale } = useComponentDefaultProps('useLocaleFormat', UseLocaleFormatDefaultProps, props);

    const [locale, setLocale] = useState(initialLocale);

    const formatValue = useCallback((value: Value, sqlType: SQLType) => formatSQLValue(value, sqlType, locale), [locale]);

    const getNativeType = useCallback((value: Value, sqlType: SQLType) => convertToNativeType(value, sqlType), []);

    const formatColumnValue = useCallback((col: EntityColumn<Value>) => {
        const result = formatSQLValue(col.value, col.type, locale)
        return result === 'null' ? '' : result;
    }, [locale]);

    return {
        locale,
        setLocale,
        formatValue,
        getNativeType,
        formatColumnValue
    };
}