export function getInitials(input: string): string {
    const cleanedInput = input.trim().toLowerCase();

    const username = cleanedInput.includes('@') ? cleanedInput.split('@')[0] : cleanedInput;

    const nameParts = username.split(/[\s._-]+/); // split by space, . _ or -

    if (nameParts.length === 1) {
        return nameParts[0].substring(0, 2).toUpperCase();
    }

    const firstInitial = nameParts[0].substring(0, 1).toUpperCase();
    const lastInitial = nameParts[nameParts.length - 1].substring(0, 1).toUpperCase();

    return `${firstInitial}${lastInitial}`;
}