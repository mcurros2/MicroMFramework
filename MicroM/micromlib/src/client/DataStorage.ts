
export interface IDataStorage {
    saveData(app_id: string, key: string, data: unknown): Promise<void>;
    readData(app_id: string, key: string): Promise<unknown | null>;
    deleteData(app_id: string, key: string): Promise<void>;
}

export class DataStorage implements IDataStorage {
    #_isAvailable;
    #storage_type;
    #storage_name;
    static #DATA_ARRAY = new Map();

    #checkStorageAvailable() {
        let storage: Storage | null = null;
        try {
            storage = window[this.#storage_type];
            if (storage === null) throw new Error(`Storage ${this.#storage_type} not available`);
            const x = '__storage_test__';
            storage.setItem(x, x);
            storage.removeItem(x);
            return true;
        }
        catch (e) {
            console.error(`${this.#storage_type} not available!`, e);
            return e instanceof DOMException && (
                // everything except Firefox
                e.code === 22 ||
                // Firefox
                e.code === 1014 ||
                // test name field too, because code might not be present
                // everything except Firefox
                e.name === 'QuotaExceededError' ||
                // Firefox
                e.name === 'NS_ERROR_DOM_QUOTA_REACHED') &&
                // acknowledge QuotaExceededError only if there's something already stored
                (storage && storage.length !== 0);
        }
    }

    constructor(storage_type:'sessionStorage'|'localStorage' = 'sessionStorage', storage_name = 'microm_data') {
        this.#storage_type = storage_type;
        this.#storage_name = storage_name;
        this.#_isAvailable = this.#checkStorageAvailable();
    }


    async saveData<T>(app_id: string, key: string, data: T) {
        const storage_key = `${this.#storage_name}_${app_id}_${key}`;

        const data_string = JSON.stringify(data);
        DataStorage.#DATA_ARRAY.set(storage_key, data_string);

        const available = this.#_isAvailable;
        if (!available || !app_id || !key) {
            console.warn('DataStorage: saveData, data not saved', app_id, key, available);
            return;
        }
        const storage = window[this.#storage_type];
        storage.setItem(storage_key, data_string);
    }

    async readData<T>(app_id: string, key: string) {
        try {
            const storage_key = `${this.#storage_name}_${app_id}_${key}`;

            if (!this.#_isAvailable) {
                console.warn('DataStorage: readData, storage not available', app_id, key);
                return JSON.parse(DataStorage.#DATA_ARRAY.get(storage_key)) as T;
            }
            const storage = window[this.#storage_type];
            const json_obj = storage.getItem(storage_key);
            return json_obj === null ? null : JSON.parse(json_obj) as T;
        }
        catch (e) {
            console.error('DataStorage: Failed to read data from DataStorage');
            return null;
        }
    }

    async deleteData(app_id: string, key: string) {
        const storage_key = `${this.#storage_name}_${app_id}_${key}`;

        DataStorage.#DATA_ARRAY.delete(storage_key);

        if (!this.#_isAvailable) {
            console.warn('DataStorage: delete data, storage not available', app_id, key);
            return;
        }
        const storage = window[this.#storage_type];
        storage.removeItem(storage_key);
    }
}
