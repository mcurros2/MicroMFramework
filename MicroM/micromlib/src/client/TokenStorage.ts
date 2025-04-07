import { MicroMToken }  from "./MicroMToken";

export interface TokenStorage {
    saveToken(app_id: string, token: MicroMToken): Promise<void>;
    readToken(app_id: string): Promise<MicroMToken | null>;
    deleteToken(app_id: string): Promise<void>;
}

export class TokenWebStorage implements TokenStorage {
    #_isAvailable;
    #storage_type;
    #storage_name;
    static #TOKEN_ARRAY = new Map();

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
            console.log('LocalStorage not available!');
            console.log(e);
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

    constructor(storage_type:'sessionStorage'|'localStorage' = 'sessionStorage', storage_name = 'microm_bk') {
        this.#storage_type = storage_type;
        this.#storage_name = storage_name;
        this.#_isAvailable = this.#checkStorageAvailable();
    }

    async saveToken(app_id: string, token: MicroMToken) {
        const key = `${this.#storage_name}_${app_id}`;
        const token_string = JSON.stringify(token);
        TokenWebStorage.#TOKEN_ARRAY.set(key, token_string);
        if (!this.#_isAvailable || !app_id) return;
        const storage = window[this.#storage_type];
        storage.setItem(key, token_string);
    }

    async readToken(app_id: string) {
        try {
            const key = `${this.#storage_name}_${app_id}`;
            if (!this.#_isAvailable) return JSON.parse(TokenWebStorage.#TOKEN_ARRAY.get(key)) as MicroMToken;
            const storage = window[this.#storage_type];
            const json_obj = storage.getItem(key);
            return json_obj === null ? null : JSON.parse(json_obj) as MicroMToken;
        }
        catch (e) {
            console.log('MicromClient: Failed to read token from localStorage');
            return null;
        }
    }

    async deleteToken(app_id: string) {
        const key = `${this.#storage_name}_${app_id}`;
        TokenWebStorage.#TOKEN_ARRAY.delete(key);
        if (!this.#_isAvailable) return;
        const storage = window[this.#storage_type];
        storage.removeItem(key);
    }
}
