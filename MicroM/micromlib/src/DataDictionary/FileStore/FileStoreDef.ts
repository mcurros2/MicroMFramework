import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_file_id: new EntityColumn<string>({ name: 'c_file_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'File Id' }),
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: c.FK, prompt: 'Fileprocess Id' }),
        vc_filename: new EntityColumn<string>({ name: 'vc_filename', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Filename' }),
        vc_filefolder: new EntityColumn<string>({ name: 'vc_filefolder', type: 'char', length: 6, flags: c.Edit, prompt: 'Filefolder' }),
        vc_fileguid: new EntityColumn<string>({ name: 'vc_fileguid', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Fileguid' }),
        bi_filesize: new EntityColumn<number>({ name: 'i_filesize', type: 'bigint', flags: c.Edit, prompt: 'Size' }),
        c_fileuploadstatus_id: new EntityColumn<string>({ name: 'c_fileuploadstatus_id', type: 'char', length: 20, flags: c.Edit, prompt: 'Fileuploadstatus Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        fst_brwStandard: { name: 'fst_brwStandard', keyMappings: { c_file_id: 0 } },
        fst_brwFiles: { name: 'fst_brwFiles', keyMappings: { c_file_id: 0, c_fileprocess_id: -1 } },
    }
)


export class FileStoreDef extends EntityDefinition {

    columns = columns();
    views = views();


    constructor() {
        super('FileStore');
    }
}