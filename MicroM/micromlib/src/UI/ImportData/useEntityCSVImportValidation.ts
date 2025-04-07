import { ValidateFileReturnType } from "../FileUploader";


export interface useEntityCSVImportValidationProps {
    errorReadingFileLabel?: string,
    emptyCSVFileLabel?: string,
    missingColumnsLabel?: string
}

export function useEntityCSVImportValidation({ errorReadingFileLabel, emptyCSVFileLabel, missingColumnsLabel }: useEntityCSVImportValidationProps) {


    const validateEntityImportCSVFile = async (props: { file: File, requiredColumns: string[] }): Promise<ValidateFileReturnType> => {
        const { file, requiredColumns } = props;
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (event) => {
                const text = event.target?.result as string;
                const result = validateCSV({ text, requiredColumns });
                resolve(result);
            };
            reader.onerror = () => {
                reject({ error: true, message: errorReadingFileLabel });
            };
            reader.readAsText(file);
        });
    };


    const validateCSV = (props: { text: string, requiredColumns: string[] }): ValidateFileReturnType => {
        const { text, requiredColumns } = props;
        const lines = text.split('\n').filter(line => line.trim() !== '');
        if (lines.length === 0) {
            return { error: true, message: emptyCSVFileLabel };
        }

        const headers = lines[0].split(',').map(header => header.trim());
        const missingColumns = requiredColumns.filter(col => !headers.includes(col));

        if (missingColumns.length > 0) {
            return { error: true, message: `${missingColumnsLabel} ${missingColumns.join(', ')}` };
        }

        return { error: false };
    };

    return {
        validateEntityImportCSVFile
    }

}