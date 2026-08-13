import { CommonFlags as c, DefaultColumns, EntityColumn, EntityDefinition } from "../../Entity";

const columns = () => ({
    vc_fileguid: new EntityColumn<string>({ name: 'vc_fileguid', type: 'varchar', length: 255, flags: c.PK, prompt: 'File GUID' }),
    c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: c.FK, prompt: 'File process ID' }),
    vc_filename: new EntityColumn<string>({ name: 'vc_filename', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Filename' }),
    vc_filefolder: new EntityColumn<string>({ name: 'vc_filefolder', type: 'char', length: 6, flags: c.Edit, prompt: 'File folder' }),
    bi_filesize: new EntityColumn<number>({ name: 'bi_filesize', type: 'bigint', flags: c.Edit, prompt: 'File size' }),
    vc_file_tag: new EntityColumn<string>({ name: 'vc_file_tag', type: 'varchar', length: 255, flags: c.Edit, prompt: 'File tag' }),
    c_fileuploadstatus_id: new EntityColumn<string>({ name: 'c_fileuploadstatus_id', type: 'char', length: 20, flags: c.Edit, prompt: 'File upload status ID' }),
    c_filestoragetype_id: new EntityColumn<string>({ name: 'c_filestoragetype_id', type: 'char', length: 20, flags: c.Edit, prompt: 'File storage type ID' }),
    ...DefaultColumns()
});

const views = () => ({
    fcc_brwFiles: { name: 'fcc_brwFiles', keyMappings: { vc_fileguid: 0, c_fileprocess_id: -1 } }
});

export class FileStoreClientDef extends EntityDefinition {
    columns = columns();
    views = views();

    constructor() {
        super('FileStoreClient');
    }
}
