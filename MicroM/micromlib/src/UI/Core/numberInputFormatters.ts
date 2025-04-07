
export function moneyFormatter (value: string, currencySymbol: string) {
    const numericValue = parseFloat(value.replace(/[^\d.-]/g, ''));

    if (Number.isNaN(numericValue)) return currencySymbol;

    if (numericValue >= 1_000_000) {
        return `${currencySymbol} ${(numericValue / 1_000_000)} M`;
    }

    if (numericValue >= 1_000) {
        return `${currencySymbol} ${(numericValue / 1_000)} K`;
    }

    return `${currencySymbol} ${numericValue.toLocaleString()}`;
};

export function moneyParser (value: string) {
    if (value.toUpperCase().endsWith('K')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_000).toString();
    }

    if (value.toUpperCase().endsWith('M')) {
        return (parseFloat(value.replace(/[^\d.-]/g, '')) * 1_000_000).toString();
    }

    return value.replace(/[^\d.-]/g, '');
};
