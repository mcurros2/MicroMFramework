import { w2grid as w2grid_original, w2event as w2event_original } from "../../../libs/w2ui/w2ui.es6";
import { GridColumnFormat, GridColumnRender } from "./Grid";

export type w2record = Record<string,unknown> & { recid:number }

export type w2recordId = string | number

export type w2event<TDetail = Record<string,unknown>> = w2event_original & {
    detail: TDetail,
}
export type w2columnAutoResizeEvent = w2event<{ maxWidth:number, originalEvent:Event, target:string, column:w2column }>

export type w2renderFunction = (this:w2grid, w2record:w2record, options:{ self:w2grid, value:unknown, index:number, colIndex:number, summary:boolean }) => string
export type w2titleFunction = (this:w2grid, w2record:w2record, options:{ self:w2grid, value:unknown, index:number, colIndex:number, summary:boolean }) => string

export type w2columnField = string // WARNING: w2ui assumes that field is string, do not allow number! Also the value can't end in "_"!!

// comments copied from w2ui
// TODO: remains to be defined object and unknown types
export type w2column = {
    text? : string | (() => string), // column text (can be a function)
    field? : w2columnField, // field name to map the column to a record
    size? : string, // size of column in px or %
    min? : number, // minimum width of column in px
    max? : number, // maximum width of column in px
    gridMinWidth? : number,  // minimum width of the grid when column is visible
    readonly sizeCorrected? : string,  // read only, corrected size (see explanation below)
    readonly sizeCalculated? : string,  // read only, size in px (see explanation below)
    sizeOriginal? : string,  // size as defined
    sizeType? : "px" | "%",  // px or %
    hidden? : boolean, // indicates if column is hidden
    sortable? : boolean, // indicates if column is sortable
    sortMode? : "default"|"natural"|"i18n"|((a:string,b:string) => number),  // sort mode ('default'|'natural'|'i18n') or custom compare function
    searchable? : boolean|string|object, // bool/string: int,float,date,... or an object to create search field
    resizable? : boolean,  // indicates if column is resizable
    hideable? : boolean,  // indicates if column can be hidden
    autoResize? : boolean,  // indicates if column can be auto-resized by double clicking on the resizer
    attr? : string,    // string that will be inside the <td ... attr> tag
    style? : string,    // additional style for the td tag
    render? : string|w2renderFunction,  // string or render function
    title? : string|w2titleFunction,  // string or function for the enableAccess property for the column cells
    tooltip? : string,  // string for the enableAccess property for the column header
    editable? : object,    // editable object (see explanation below)
    frozen? : boolean, // indicates if the column is fixed to the left
    info? : boolean|object,  // info bubble, can be bool/object
    clipboardCopy? : boolean|string|((...args: unknown[]) => unknown), // if true (or string or function), it will display clipboard copy icon

    // These are not part of w2ui, we use them to detect changes
    // TODO: Move this to component
    _microm_format?: GridColumnFormat,
    _microm_render?: GridColumnRender,
    _microm_autoSizeMax?: number,
    _microm_autoSize_cache?: boolean,
    _microm_userSize_cache?: boolean,
}

export class w2grid extends w2grid_original {
    columns!: w2column[];
    //getColumn(field:w2columnField): w2column|undefined
    getColumn(field:w2columnField): w2column|undefined { return super.getColumn(field, false); }
    getColumnIndex(field:w2columnField): number|undefined { return super.getColumn(field, true); }
    addColumn(columns: w2column|w2column[], before?: string|number): number { return super.addColumn(before, columns); }
}
