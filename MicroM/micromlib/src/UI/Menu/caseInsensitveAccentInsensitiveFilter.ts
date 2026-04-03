import { SpotlightAction } from "@mantine/spotlight";

export function normalizeTextToLowercaseWithoutAccents(value: string) {
    return value
        .normalize("NFD") // separate characters from their accents
        .replace(/[\u0300-\u036f]/g, "") // delete the accents
        .toLowerCase(); // case insensitive
}

export function caseInsensitveAccentInsensitiveFilter(query: string, actions: SpotlightAction[]) {
    const tokens = normalizeTextToLowercaseWithoutAccents(query)
        .split(/\s+/)
        .filter(Boolean);

    return actions.filter((action) => {
        const label = normalizeTextToLowercaseWithoutAccents(action.label || "");
        const description = normalizeTextToLowercaseWithoutAccents(action.description || "");

        const text = `${label} ${description}`;

        return tokens.every((token) => text.includes(token));
    });
}