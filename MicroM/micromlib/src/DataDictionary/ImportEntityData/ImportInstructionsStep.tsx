import { Button, Group, Stack, Table, Text } from "@mantine/core";
import { IconFileSpreadsheet, IconFileTypeCsv } from "@tabler/icons-react";
import ExcelJS from "exceljs";
import { SQLType, Value } from "../../client";
import { Entity, EntityColumn, EntityDefinition } from "../../Entity";

export interface ImportInstructionsStepProps {
    importEntity?: Entity<EntityDefinition>,
    requiredColumns: readonly string[],
    instructionsLabel?: string,
    columnHeaderLabel?: string,
    dataNameLabel?: string,
    dataTypeLabel?: string,
    contentLabel?: string,
    dateFormatLabel?: string,
    numberFormatLabel?: string,
    downloadExcelSampleLabel?: string,
    downloadCSVSampleLabel?: string,
}

export function getFriendlyImportDataType(type: SQLType) {
    if (['tinyint', 'smallint', 'int', 'bigint', 'bit'].includes(type)) return 'Integer';
    if (['float', 'decimal', 'real', 'money'].includes(type)) return 'Number';
    if (['date', 'datetime', 'datetime2', 'smalldatetime', 'time'].includes(type)) return 'Date';
    return 'Alphanumeric';
}

function downloadBlob(blob: Blob, fileName: string) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

function escapeCSVValue(value: string) {
    return `"${value.replace(/"/g, '""')}"`;
}

export function ImportInstructionsStep({
    importEntity,
    requiredColumns,
    instructionsLabel = 'CSV and Excel files must include a header row. These columns can be mapped after selecting a file:',
    columnHeaderLabel = 'Column header',
    dataNameLabel = 'Data name',
    dataTypeLabel = 'Data type',
    contentLabel = 'Content',
    dateFormatLabel = '* Dates should use the format YYYY-MM-DD.',
    numberFormatLabel = "* Numbers with decimal places should use '.' as the separator.",
    downloadExcelSampleLabel = 'Download Excel sample',
    downloadCSVSampleLabel = 'Download CSV sample'
}: ImportInstructionsStepProps) {
    const downloadCSVSample = () => {
        if (!importEntity) return;
        const content = `\uFEFF${requiredColumns.map(escapeCSVValue).join(',')}\r\n`;
        downloadBlob(new Blob([content], { type: 'text/csv;charset=utf-8' }), `${importEntity.name}_sample.csv`);
    };

    const downloadExcelSample = async () => {
        if (!importEntity) return;
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet('Import');
        worksheet.addRow([...requiredColumns]);
        const buffer = await workbook.xlsx.writeBuffer();
        downloadBlob(
            new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }),
            `${importEntity.name}_sample.xlsx`
        );
    };

    return (
        <Stack spacing="sm">
            <Text size="sm">{instructionsLabel}</Text>
            <Table striped withBorder withColumnBorders width="100%">
                <thead>
                    <tr>
                        <th>{columnHeaderLabel}</th>
                        <th>{dataNameLabel}</th>
                        <th>{dataTypeLabel}</th>
                        <th>{contentLabel}</th>
                    </tr>
                </thead>
                <tbody>
                    {requiredColumns.map(columnName => {
                        const column = importEntity?.def.columns[columnName] as EntityColumn<Value> | undefined;
                        if (!column) return null;
                        return (
                            <tr key={column.name}>
                                <td>{column.name}</td>
                                <td>{column.prompt}</td>
                                <td>{getFriendlyImportDataType(column.type)}</td>
                                <td>{column.description}</td>
                            </tr>
                        );
                    })}
                </tbody>
            </Table>
            <Stack spacing={0}>
                <Text size="xs" color="dimmed">{dateFormatLabel}</Text>
                <Text size="xs" color="dimmed">{numberFormatLabel}</Text>
            </Stack>
            <Group>
                <Button variant="outline" leftIcon={<IconFileSpreadsheet size="1rem" />} onClick={() => void downloadExcelSample()}>
                    {downloadExcelSampleLabel}
                </Button>
                <Button variant="outline" leftIcon={<IconFileTypeCsv size="1rem" />} onClick={downloadCSVSample}>
                    {downloadCSVSampleLabel}
                </Button>
            </Group>
        </Stack>
    );
}
