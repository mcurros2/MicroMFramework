
export function moneyFormatter(value: string, currencySymbol: string) {
    const numericValue = parseFloat(value.replace(/[^\d.-]/g, ''));

    if (Number.isNaN(numericValue)) return currencySymbol;

    const currency = currencySymbol ? `${currencySymbol} ` : '';

    if (numericValue >= 1_000_000) {
        return `${currency}${(numericValue / 1_000_000)} M`;
    }

    if (numericValue >= 1_000) {
        return `${currency}${(numericValue / 1_000)} K`;
    }

    return `${currency}${numericValue.toLocaleString()}`;
};

export function moneyParser(value: string) {
    if (value.toUpperCase().endsWith('K')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_000).toString();
    }

    if (value.toUpperCase().endsWith('M')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_000_000).toString();
    }

    return value.replace(/[^\d.-]/g, '');
};

export function bytesFormatter(value: string) {
    const numericValue = parseFloat(value.replace(/[^\d.-]/g, ''));

    if (Number.isNaN(numericValue)) return '';

    if (numericValue >= 1_073_741_824) {
        return `${(numericValue / 1_073_741_824).toFixed(2)} GB`;
    }

    if (numericValue >= 1_048_576) {
        return `${(numericValue / 1_048_576).toFixed(2)} MB`;
    }

    if (numericValue >= 1_024) {
        return `${(numericValue / 1_024).toFixed(2)} KB`;
    }

    return `${numericValue} B`;
};

export function bytesParser(value: string) {
    if (value.toUpperCase().endsWith('KB')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_024).toString();
    }

    if (value.toUpperCase().endsWith('MB')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_048_576).toString();
    }

    if (value.toUpperCase().endsWith('GB')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_073_741_824).toString();
    }

    return value.replace(/[^\d.-]/g, '');
};
