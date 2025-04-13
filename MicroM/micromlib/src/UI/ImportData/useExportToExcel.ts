import { DataResult } from '../../client/client.types';
import { exportToExcel } from '../../Entity/DataResultFunctions';

export interface UseExportToExcelProps {
    data: DataResult[]; // contains an array of DataResult
    dataResultNames?: string[]; // Optional contains the name for each worksheet for each DataResult, if no will create Data 1, Data 2, etc.
    notExportableColumns?: number[]
}

export function useExportToExcel(props: UseExportToExcelProps) {
    const { data, dataResultNames, notExportableColumns } = props;
    const exportToExcelWrapper = async () => {
        await exportToExcel(data, notExportableColumns, dataResultNames);
    };
    return { exportToExcel: exportToExcelWrapper };
}