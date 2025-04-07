import { DataResult, Value } from "./client.types";

export class RecordReader {
    private dataResult: DataResult;
    private headerIndexCache: { [key: string]: number };

    constructor(dataResult: DataResult) {
        this.dataResult = dataResult;
        this.headerIndexCache = this.createHeaderIndexCache();
    }

    private createHeaderIndexCache(): { [key: string]: number } {
        const cache: { [key: string]: number } = {};
        this.dataResult.Header.forEach((header, index) => {
            cache[header] = index;
        });
        return cache;
    }

    getValue(recordIndex: number, headerName: string): Value | undefined {
        const headerIndex = this.headerIndexCache[headerName];
        if (headerIndex === undefined || recordIndex >= this.dataResult.records.length) {
            return undefined;
        }

        const record = this.dataResult.records[recordIndex];
        return record[headerIndex];
    }
}
