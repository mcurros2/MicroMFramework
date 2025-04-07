import { SQLType, Value } from "../client";
import { EntityColumnFlags, EntityColumnOptions } from "./EntityColumn.types";

export function checkAdmittedValue(col: EntityColumn<Value>, value: Value, throwException: boolean = false): boolean {
    let ret: boolean = false;
    if (['char', 'nchar', 'varchar', 'nvarchar', 'text', 'ntext'].includes(col.type)) {
        if (typeof value !== "string" || (!col.hasFlag(EntityColumnFlags.nullable) && value === null))
            ret = false;
        else
            ret = true;
    }
    if (['int', 'bigint', 'float', 'decimal', 'real', 'money'].includes(col.type)) {
        if (typeof value !== "number" || (!col.hasFlag(EntityColumnFlags.nullable) && value === null))
            ret = false;
        else
            ret = true;
    }
    if (['datetime', 'date'].includes(col.type)) {
        if (typeof value !== typeof Date || (!col.hasFlag(EntityColumnFlags.nullable) && value === null))
            ret = false;
        else
            ret = true;
    }
    if (col.type === "bit") {
        if (typeof value !== typeof Boolean || (!col.hasFlag(EntityColumnFlags.nullable) && value === null))
            ret = false;
        else
            ret = true;
    }
    if (throwException) throw new Error(`Invalid value for ${col.name}. Column type is ${col.type} and nullable is ${col.hasFlag(EntityColumnFlags.nullable)} and value is ${value}`);
    return ret;
}

export class EntityColumn<T extends Value> {
    name: string;
    type: SQLType;
    length: number;
    scale: number;
    flags: EntityColumnFlags;
    prompt: string;
    placeholder: string;
    description: string;
    isArray: boolean;
    excludeInAutoForm: boolean;

    constructor({ name, type, length, scale, value, defaultValue, flags, prompt, placeholder, description, isArray, excludeInAutoForm, valueDescription }: EntityColumnOptions<T>) {
        this._value = null as T;
        this._defaultValue = null as T;
        this.name = name;
        this.type = type;
        this.length = (length) ? parseInt(length.toFixed()) : 0;
        this.scale = (scale) ? parseInt(scale.toFixed()) : 0;
        this.defaultValue = (defaultValue) ? defaultValue as T : null as T;
        this.value = (value) ? value as T : this.defaultValue as T;
        this.flags = flags;
        this.prompt = (prompt) ? prompt : '';
        this.placeholder = placeholder ?? '';
        this.description = description ?? '';
        this.isArray = isArray ?? false;
        this.excludeInAutoForm = excludeInAutoForm ?? false;
        this._valueDescription = valueDescription;
    }

    private _value: T;
    get value() { return this._value as T; }
    set value(newValue: T) {
        checkAdmittedValue(this, newValue);
        const oldValue = this._value;
        if (newValue !== oldValue) {
            this._value = newValue;
        }
    }

    private _defaultValue: T;
    get defaultValue() { return this._defaultValue as T; }
    set defaultValue(newValue: T) {
        checkAdmittedValue(this, newValue);
        this._defaultValue = newValue;
    }

    private _valueDescription: string | undefined;
    get valueDescription() { return this._valueDescription; }
    set valueDescription(newValue: string | undefined) {
        this._valueDescription = newValue;
    }

    hasFlag(flagOption: EntityColumnFlags) { return ((this.flags & flagOption) === flagOption); }

    hasAnyFlag(combinedFlags: EntityColumnFlags) { return (this.flags & combinedFlags) !== 0; }

    static clone(source: EntityColumn<Value>) {
        return new EntityColumn({
            name: source.name,
            type: source.type,
            length: source.length,
            scale: source.scale,
            value: source.value,
            defaultValue: source.defaultValue,
            flags: source.flags,
            prompt: source.prompt,
            placeholder: source.placeholder,
            description: source.description,
            isArray: source.isArray,
            excludeInAutoForm: source.excludeInAutoForm,
            valueDescription: source.valueDescription
        });
    }
}

