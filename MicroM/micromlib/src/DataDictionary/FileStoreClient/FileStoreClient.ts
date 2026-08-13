import { DBStatusResult, MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { FileStoreClientDef } from "./FileStoreClientDef";

export interface FileStoreClientFile {
    vc_fileguid: string;
    c_fileprocess_id: string;
    vc_filename: string;
    vc_filefolder: string;
    bi_filesize: number;
    vc_file_tag?: string;
    c_fileuploadstatus_id: string;
    c_filestoragetype_id?: string;
    documentURL: string;
    thumbnailURL: string;
}

export class FileStoreClient extends Entity<FileStoreClientDef> {
    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreClientDef(), parentKeys);
    }

    async listFiles(fileProcessID: string, maxSize = 150, quality = 75, abortSignal: AbortSignal | null = null): Promise<FileStoreClientFile[]> {
        if (!fileProcessID) return [];

        const data = await this.API.executeView(
            this.def.views.fcc_brwFiles,
            { c_fileprocess_id: fileProcessID },
            null,
            null,
            abortSignal
        );

        return data.flatMap(result => result.records.map(record => {
            const value = (name: string) => record[result.Header.indexOf(name)];
            const guid = String(value('vc_fileguid') ?? '');

            return {
                vc_fileguid: guid,
                c_fileprocess_id: String(value('c_fileprocess_id') ?? ''),
                vc_filename: String(value('vc_filename') ?? ''),
                vc_filefolder: String(value('vc_filefolder') ?? ''),
                bi_filesize: Number(value('bi_filesize') ?? 0),
                vc_file_tag: value('vc_file_tag') == null ? undefined : String(value('vc_file_tag')),
                c_fileuploadstatus_id: String(value('c_fileuploadstatus_id') ?? ''),
                c_filestoragetype_id: value('c_filestoragetype_id') == null ? undefined : String(value('c_filestoragetype_id')),
                documentURL: this.API.client.getDocumentURL(guid),
                thumbnailURL: this.API.client.getThumbnailURL(guid, maxSize, quality)
            };
        })).filter(file => file.vc_fileguid);
    }

    async deleteFile(fileGUID: string, abortSignal: AbortSignal | null = null): Promise<DBStatusResult> {
        this.def.columns.vc_fileguid.value = fileGUID;
        return await this.API.deleteData(undefined, abortSignal);
    }
}
