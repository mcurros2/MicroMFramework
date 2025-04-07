import ExcelJS from 'exceljs';
import { DataResult } from '../../client/client.types';

export interface UseExportToExcelProps {
    data: DataResult[]; // contains an array of DataResult
    dataResultNames?: string[]; // Optional contains the name for each worksheet for each DataResult, if no will create Data 1, Data 2, etc.
}

export function useExportToExcel(props: UseExportToExcelProps) {
    const { data, dataResultNames } = props;
    const exportToExcel = async () => {
        const fechaActual = new Date();
        const filename = `export-${fechaActual.getFullYear()}${(fechaActual.getDate() + '').padStart(2, '0')}${(fechaActual.getMonth() + 1 + '').padStart(2, '0')}_${(fechaActual.getHours() + '').padStart(2, '0')}${(fechaActual.getMinutes() + '').padStart(2, '0')}${(fechaActual.getSeconds() + '').padStart(2, '0')}.xlsx`;

        const workbook = new ExcelJS.Workbook();
        data.forEach((result, index) => {
            const worksheet = workbook.addWorksheet(dataResultNames ? dataResultNames[index] : `Data ${index + 1}`);
            const columns = result.Header.map(header => ({ header, key: header }));
            worksheet.columns = columns;
            result.records.forEach(row => worksheet.addRow(row));
        });
        const buffer = await workbook.xlsx.writeBuffer();
        const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    };
    return { exportToExcel };
}