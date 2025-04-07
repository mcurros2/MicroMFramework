
export function JSONDateWithTimezoneReplacer(this: any, key: string, value: unknown): unknown {
    if (this[key] instanceof Date) {
        const date = this[key] as Date;

        // Obtener componentes de la fecha en la zona horaria local
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0'); // Los meses empiezan en 0
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const seconds = String(date.getSeconds()).padStart(2, '0');
        const milliseconds = String(date.getMilliseconds()).padStart(3, '0');

        // Obtener el offset de la zona horaria local
        const timezoneOffset = -date.getTimezoneOffset();
        const sign = timezoneOffset >= 0 ? '+' : '-';
        const offsetHours = String(Math.floor(Math.abs(timezoneOffset) / 60)).padStart(2, '0');
        const offsetMinutes = String(Math.abs(timezoneOffset) % 60).padStart(2, '0');
        const offset = `${sign}${offsetHours}:${offsetMinutes}`;

        // Formatear la fecha con la información de la zona horaria
        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.${milliseconds}${offset}`;
    }
    return value;
}
