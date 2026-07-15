import { isIn } from "../Entity/GenericFunctions";
import { DataResult, DBStatusResult, ValuesObject } from "./client.types";
import { DataStorage } from "./DataStorage";
import { ImpDataResult } from "./ImpDataResult";
import { JSONDateWithTimezoneReplacer } from "./JSONDateWithTimezoneReplacer";
import { AllowedRouteFlags, generateCSharpGetRoutePaths, RECORDED_ACCESS_DATA_STORAGE_KEY, RecordedAccessData } from "./MenuRoutesMapping";
import { MicroMError } from "./MicroMError";
import { MicroMClientClaimTypes, MicroMToken } from "./MicroMToken";
import { PublicEndpoint } from "./PublicEndpoint";
import { TimeoutSignal } from "./TimeoutSignal";
import { TokenStorage, TokenWebStorage } from "./TokenStorage";
import { TotpSetupStartResponse } from "./TotpSetupStartResponse";
import { TotpAuthenticatorsResponse } from "./TotpAuthenticatorResponse";
import { TwoFactorLoginResult } from "./TwoFactorLoginResult";

export type APIAction = "get" | "insert" | "update" | "delete" | "lookup" | "view" | "viewstream" | "action" | "upload" | "proc" | "procstream" | "process" | "import" | "viewtoexcel" | "proctoexcel" | "timezoneoffset";

export interface FileUploadResponse {
    ErrorMessage?: string,
    FileProcessId?: string,
    FileId?: string,
    FileGuid?: string,
    documentURL?: string,
    thumbnailURL?: string,
}

export type FetchUploadProgress = (file: File, progress: number) => void;

export interface MicroMClientProps {
    api_url: string
    , app_id: string
    , login_timeout?: number
    , mode?: RequestMode
    , tokenStorage?: TokenStorage
    , publicEndpoints?: Record<string, PublicEndpoint>
    , dataStorage?: DataStorage
    , redirect_on_401?: string
}

const ENABLED_MENUS_DATA_KEY = 'mm_menus';

const REMEMBER_USER_DATA_KEY = 'mm_remember_me';

const TIMEZONE_OFFSET_DATA_KEY = 'mm_server_timezone_offset';

const LOCAL_DEVICE_ID_KEY = 'mm_ldid';


export class MicroMClient {
    #API_URL;
    #TOKEN_STORAGE;
    #APP_ID;
    #LOGIN_TIMEOUT;
    #REQUEST_MODE;
    #TOKEN: MicroMToken | null = null;
    #LOCAL_DEVICE_ID: string | null = null;
    #tokenRefreshInProgress: Promise<void> | null = null;
    #publicEndpoints: Record<string, PublicEndpoint> = {};
    #DATA_STORAGE;
    #ENABLED_MENUS: Set<string> = new Set<string>();

    #RECORD_PATHS: boolean = false;
    #RECORDED_PATHS: Record<string, RecordedAccessData> = {};

    #REDIRECT_ON_401?: string;

    #TIMEZONE_OFFSET: number = 0;

    constructor
        (
            { api_url, app_id, login_timeout, mode, tokenStorage, publicEndpoints, dataStorage, redirect_on_401 }: MicroMClientProps
        ) {

        this.#API_URL = api_url ?? "";
        this.#APP_ID = app_id ?? "";
        this.#TOKEN_STORAGE = tokenStorage ?? new TokenWebStorage("localStorage");
        this.#LOGIN_TIMEOUT = login_timeout ?? 30000;
        this.#REQUEST_MODE = mode ?? 'cors';
        this.#publicEndpoints = publicEndpoints ?? {};
        this.#DATA_STORAGE = dataStorage ?? new DataStorage("localStorage");
        this.#REDIRECT_ON_401 = redirect_on_401;
    }

    getAPPID() { return this.#APP_ID; }

    //*** Record access
    startRecordPaths() {
        this.#RECORD_PATHS = true;
    }

    stopRecordPaths() {
        this.#RECORD_PATHS = false;
    }

    #recordAccess({ entityName, access, views, procs, actions, lookups }: RecordedAccessData) {
        if (this.#RECORD_PATHS) {
            const rec = this.#RECORDED_PATHS[entityName];
            if (rec) {
                rec.access = rec.access | access;

                if (views) rec.views = [...new Set([...(rec.views || []), ...views])];
                if (procs) rec.procs = [...new Set([...(rec.procs || []), ...procs])];
                if (actions) rec.actions = [...new Set([...(rec.actions || []), ...actions])];
                if (lookups) rec.lookups = [...new Set([...(rec.lookups || []), ...lookups])];

            } else {
                this.#RECORDED_PATHS[entityName] = { entityName, access, views, procs, actions };
            }
        }
    }

    async #saveRecordedAccessData(data: string) {
        await this.#DATA_STORAGE.saveData(this.#APP_ID, RECORDED_ACCESS_DATA_STORAGE_KEY, data);
    }

    async #saveAllRecordedAccess() {
        if (this.#RECORD_PATHS) {
            const result = generateCSharpGetRoutePaths(this.#RECORDED_PATHS);
            await this.#saveRecordedAccessData(result);
            console.log(result);
        }
    }

    //*** OIDC flows
    async oidcLoginAndRedirect(options: { targetLinkUri?: string; extraParams?: Record<string, string> }) {
        const authorizeUrl = await this.#startOidcLogin(options);
        window.location.href = authorizeUrl;
    }

    async #startOidcLogin(options: {
        targetLinkUri?: string;
        extraParams?: Record<string, string>;
    }): Promise<string> {
        const { targetLinkUri, extraParams } = options;

        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'OIDC PAR request timed out');

        try {
            const localDeviceId = await this.#getLocalDeviceId();

            // Prepare OIDC client PAR request parameters
            // required OIDC parameters are server-managed
            const params = new URLSearchParams();
            if (targetLinkUri) params.set('target_link_uri', targetLinkUri);
            params.set('local_device_id', localDeviceId);
            params.set('client_id', this.#APP_ID);

            if (extraParams) {
                for (const [k, v] of Object.entries(extraParams)) {
                    if (v != null) {
                        // Avoid overriding security-related parameters that are server-managed.
                        if (isIn<string>(k, 'response_type', 'scope', 'state', 'nonce', 'code_challenge', 'code_challenge_method', 'client_id', 'request_uri', 'redirect_uri')) {
                            console.warn(`Ignoring OIDC extraParam '${k}' because it is server-managed.`);
                            continue;
                        }
                        params.set(k, v);
                    }
                }
            }

            const parUrl = `${this.#API_URL}/${this.#APP_ID}/oidc-client/par`;

            const response = await fetch(parUrl, {
                method: 'POST',
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: params.toString()
            });

            if (!response.ok) {
                const text = await response.text().catch(() => '');
                throw {
                    status: response.status,
                    statusMessage: response.statusText,
                    message: text || response.statusText,
                    url: response.url
                } as MicroMError;
            }

            const data = await response.json() as { authorize_url?: string; error?: string; error_description?: string };

            if (!data.authorize_url) {
                throw {
                    status: 500,
                    statusMessage: 'invalid_par_response',
                    message: data.error_description || data.error || 'PAR response missing authorize_url',
                    url: parUrl
                } as MicroMError;
            }

            return data.authorize_url;
        }
        finally {
            loginTimeout.clear();
        }
    }

    //*** Local login/logout
    get LOGGED_IN_USER(): Partial<MicroMClientClaimTypes> | undefined { return this.#TOKEN?.claims; }

    async getRememberUser(): Promise<string | null> {
        return await this.#DATA_STORAGE.readData(this.#APP_ID, REMEMBER_USER_DATA_KEY);
    }

    async isLoggedIn(): Promise<boolean> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Login request timed out');

        try {

            await this.#checkAndRefreshToken();

            if (this.#TOKEN !== null && this.#TOKEN.access_token !== '') {
                const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/isloggedin`, {
                    method: 'GET',
                    headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                    mode: this.#REQUEST_MODE,
                    cache: 'no-store',
                    credentials: 'include',
                    referrerPolicy: 'strict-origin-when-cross-origin',
                    signal: loginTimeout.signal
                });

                return response.ok;
            }
        }
        catch {
            //console.log(error);
        }
        finally {
            loginTimeout.clear();
        }
        return false;
    }

    async isLoggedInLocal(): Promise<boolean> {
        try {
            await this.#checkAndRefreshToken();
        }
        catch {
            //console.log(error)
        }
        return !!this.#TOKEN?.access_token && new Date() < new Date(this.#TOKEN.expiration);
    }

    async login(username: string, password: string, rememberme?: boolean): Promise<MicroMToken | TwoFactorLoginResult> {
        //Intentionally not accounting for tokenRefreshInProgress (see refresh logic)

        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Login request timed out');
        try {

            if (this.#RECORD_PATHS) {
                this.#RECORDED_PATHS = {};
            }

            const localDeviceId = await this.#getLocalDeviceId();

            const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/login`, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ username: username, password: password, LocalDeviceId: localDeviceId })
            });

            if (!response.ok) {
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url } as MicroMError;
            }

            const data = await response.json();

            if (data?.requires_two_factor && data?.two_factor_challenge_id) {
                return data as TwoFactorLoginResult;
            }

            if (data && data.access_token) {
                const token = new MicroMToken(data.access_token, data.expires_in, data['refresh-token'], data.token_type, data);
                await this.#setToken(token);

                try {
                    await this.#DATA_STORAGE.saveData(this.#APP_ID, REMEMBER_USER_DATA_KEY, rememberme ? username : null);
                }
                catch (error) {
                    console.warn('Error remembering user', error);
                }

                try {
                    await this.#getAPIEnabledMenus(username, loginTimeout.signal);
                }
                catch (error) {
                    console.warn('Error getting enabled menus', error);
                }

                try {
                    await this.#getTimeZoneOffset(loginTimeout.signal);
                }
                catch (error) {
                    console.warn('Error getting server timezone offset', error);
                }

                return token;
            } else {
                throw { statusMessage: 'Unexpected result', url: response.url } as MicroMError;
            }
        }
        catch (error) {
            console.warn('Login error', error);
            try {
                await this.logoff();
            }
            catch { }
            throw error;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async login2fa(challengeId: string, code: string, rememberme?: boolean, username?: string): Promise<MicroMToken> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Two-factor login request timed out');
        try {
            const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/login-2fa`, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ challengeId, code })
            });

            if (!response.ok) {
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url } as MicroMError;
            }

            const data = await response.json();
            if (data && data.access_token) {
                const token = new MicroMToken(data.access_token, data.expires_in, data['refresh-token'], data.token_type, data);
                await this.#setToken(token);

                try {
                    await this.#DATA_STORAGE.saveData(this.#APP_ID, REMEMBER_USER_DATA_KEY, rememberme ? username ?? null : null);
                }
                catch (error) {
                    console.warn('Error remembering user', error);
                }

                try {
                    await this.#getAPIEnabledMenus(username ?? token.claims.username ?? '', loginTimeout.signal);
                }
                catch (error) {
                    console.warn('Error getting enabled menus', error);
                }

                try {
                    await this.#getTimeZoneOffset(loginTimeout.signal);
                }
                catch (error) {
                    console.warn('Error getting server timezone offset', error);
                }

                return token;
            }

            throw { statusMessage: 'Unexpected result', url: response.url } as MicroMError;
        }
        catch (error) {
            console.warn('Two-factor login error', error);
            throw error;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async registerLoginTotp(challengeId: string): Promise<TotpSetupStartResponse> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Authenticator registration request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/login-2fa/register`;

        try {
            const response = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ challengeId })
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }

            return await response.json() as TotpSetupStartResponse;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async sendTwoFactorEmailCode(challengeId: string): Promise<void> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Two-factor email code request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/login-2fa/email-code`;

        try {
            const response = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ challengeId })
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }
        }
        finally {
            loginTimeout.clear();
        }
    }

    async localLogoff() {
        await this.#removeToken();
        await this.#deleteEnabledMenus();
    }

    async logoff() {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Login request timed out');

        try {
            await this.#removeToken();
            await this.#deleteEnabledMenus();

            if (this.#RECORD_PATHS) {
                await this.#saveAllRecordedAccess();
            }

            const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/logoff`, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN?.access_token || ''}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({})
            });

            if (!response.ok) {
                throw { status: response.status, statusMessage: response.statusText, message: response.statusText, url: response.url } as MicroMError;
            }

        }
        finally {
            loginTimeout.clear();
        }
    }

    async updateClientClaims(claims: Partial<MicroMClientClaimTypes>) {
        if (this.#TOKEN) {
            this.#TOKEN.claims = claims;
            await this.#setToken(this.#TOKEN);
        }
    }

    async recoveryemail(username: string): Promise<DBStatusResult> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT * 4, 'Login request timed out');

        try {
            const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/recoveryemail`, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ username: username })
            });

            if (!response.ok) {
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url } as MicroMError;
            }

            return await response.json();
        }
        catch (error) {
            throw error;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async recoverpassword(username: string, password: string, recoverycode: string): Promise<DBStatusResult> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT * 4, 'Login request timed out');
        try {

            const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/recoverpassword`, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8" },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ username: username, password: password, recoverycode: recoverycode })
            });

            if (!response.ok) {
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url } as MicroMError;
            }

            return await response.json();
        }
        catch (error) {
            throw error;
        }
        finally {
            loginTimeout.clear();
        }

    }

    async startTotpSetup(authenticatorName: string): Promise<TotpSetupStartResponse> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'TOTP setup request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/totp/setup`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const response = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ authenticatorName })
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }

            return await response.json() as TotpSetupStartResponse;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async confirmTotpSetup(code: string, setupChallengeId: string): Promise<void> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'TOTP confirmation request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/totp/confirm`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const response = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ Code: code, SetupChallengeId: setupChallengeId })
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }
        }
        finally {
            loginTimeout.clear();
        }
    }

    async listTotpAuthenticators(): Promise<TotpAuthenticatorsResponse> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'TOTP authenticators request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/totp/authenticators`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const response = await fetch(route, {
                method: 'GET',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }

            return await response.json() as TotpAuthenticatorsResponse;
        }
        finally {
            loginTimeout.clear();
        }
    }

    async deleteTotpAuthenticator(authenticatorId: string): Promise<void> {
        const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Delete authenticator request timed out');
        const route = `${this.#API_URL}/${this.#APP_ID}/auth/totp/authenticators/delete`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const response = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: loginTimeout.signal,
                body: JSON.stringify({ authenticatorId })
            });

            if (!response.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await response.text(); } catch { }
                throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url, errorBody: error_body } as MicroMError;
            }
        }
        finally {
            loginTimeout.clear();
        }
    }

    async #getLocalDeviceId() {
        const local_device: string | null = await this.#DATA_STORAGE.readData(this.#APP_ID, LOCAL_DEVICE_ID_KEY);
        if (local_device !== null) {
            this.#LOCAL_DEVICE_ID = local_device;
            return local_device;
        }

        // Generate a new local device ID if it doesn't exist
        const randomId = "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
            (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
        );

        await this.#DATA_STORAGE.saveData(this.#APP_ID, LOCAL_DEVICE_ID_KEY, randomId);
        this.#LOCAL_DEVICE_ID = randomId;
        return randomId;
    }

    async #loadToken() {
        if (!this.#TOKEN) {
            this.#TOKEN = await this.#TOKEN_STORAGE.readToken(this.#APP_ID);
        }
    }

    async #setToken(token: MicroMToken) {
        await this.#TOKEN_STORAGE.saveToken(this.#APP_ID, token);
        this.#TOKEN = token;
    }

    async #removeToken() {
        await this.#TOKEN_STORAGE.deleteToken(this.#APP_ID);
        this.#TOKEN = null;
    }

    async #checkAndRefreshToken() {
        await this.#loadToken();
        await this.#readTimeZoneOffset();

        if (!this.#TOKEN) {
            return;
        }
        const renewalAheadPeriod = 1 * 60 * 1000;
        const now = new Date();
        const expiration = new Date(this.#TOKEN.expiration);
        if (now > new Date(expiration.getTime() - renewalAheadPeriod)) {
            await this.#refreshToken();
        }
    }

    async #refreshToken(): Promise<void> {
        // Check if a token refresh is already in progress.
        // If so, return that promise instead of starting a new request.
        if (this.#tokenRefreshInProgress) {
            console.warn('Token refresh already in progress');
            return this.#tokenRefreshInProgress;
        }

        const _performTokenRefresh = async () => {
            const loginTimeout = new TimeoutSignal(this.#LOGIN_TIMEOUT, 'Login request timed out');

            try {

                if (!this.#TOKEN) { throw new Error('Token not found'); }

                //console.log('Refreshing token');
                const old_token = this.#TOKEN;
                const response = await fetch(`${this.#API_URL}/${this.#APP_ID}/auth/refresh`, {
                    method: 'POST',
                    headers: { "Content-Type": "application/json; charset=utf-8" },
                    mode: this.#REQUEST_MODE,
                    cache: 'no-store',
                    credentials: 'include',
                    referrerPolicy: 'strict-origin-when-cross-origin',
                    signal: loginTimeout.signal,
                    body: JSON.stringify({ Bearer: this.#TOKEN.access_token, RefreshToken: this.#TOKEN.refresh_token })
                });

                if (!response.ok) {
                    throw { status: response?.status, statusMessage: response?.statusText, message: response?.statusText, url: response?.url } as MicroMError;
                }

                const data = await response.json();

                if (data && data.access_token) {
                    const new_token = new MicroMToken(data.access_token, data.expires_in, data['refresh-token'], data.token_type, this.#TOKEN.claims);
                    if (old_token !== this.#TOKEN) {
                        throw new Error("Current token was changed while refreshing.");
                    }
                    await this.#setToken(new_token);
                    //console.log('Token refreshed', new_token);
                    return;
                } else {
                    //console.log('Unexpected result', data);
                    throw { statusMessage: 'Unexpected result', url: response.url } as MicroMError;
                }
            }
            catch (error) {
                //console.log('RefreshToken', error);
                throw error;
            } finally {
                this.#tokenRefreshInProgress = null; // Clear the ongoing refresh promise once done.
                loginTimeout.clear();
            }
        }

        this.#tokenRefreshInProgress = _performTokenRefresh();
        return this.#tokenRefreshInProgress;
    }


    //*** Timezone offset
    get TIMEZONE_OFFSET(): number { return this.#TIMEZONE_OFFSET; }

    async #saveTimeZoneOffset(data: number) {
        await this.#DATA_STORAGE.saveData(this.#APP_ID, TIMEZONE_OFFSET_DATA_KEY, data);
    }

    async #readTimeZoneOffset(): Promise<number> {
        const result: number | null = await this.#DATA_STORAGE.readData(this.#APP_ID, TIMEZONE_OFFSET_DATA_KEY);
        if (result === null) {
            console.warn('No timezone offset found, using default 0');
            this.#TIMEZONE_OFFSET = 0;
            return 0;
        }
        this.#TIMEZONE_OFFSET = result;
        return result;
    }

    async #getTimeZoneOffset(abort_signal: AbortSignal | null = null) {
        if (!this.#TOKEN) return;

        const result: DataResult[] = await this.#submitToAPI('SystemProcs', null, {}, [], 'proc', abort_signal, 'sys_GetTimeZoneOffset');

        const offset = result[0].records[0][0] as number;

        if (isNaN(offset)) {
            console.warn('No timezone offset found, using default 0');
            return 0;
        }

        await this.#saveTimeZoneOffset(offset);

        return result;
    }


    //*** Enabled menus
    async getMenus(): Promise<Set<string>> {

        if (this.#ENABLED_MENUS.size === 0) {
            const data = await this.#readEnabledMenus();
            this.#ENABLED_MENUS = data ? new Set<string>(data) : new Set<string>();
        }

        return this.#ENABLED_MENUS;
    }

    async #getAPIEnabledMenus(username: string, abort_signal: AbortSignal | null = null) {
        if (!this.#TOKEN) return;

        const result: DataResult[] = await this.#submitToAPI('MicromUsers', null, { vc_username: username }, [], 'proc', abort_signal, 'usr_GetEnabledMenus');

        if (result.length === 0 || result[0].records.length === 0) throw { status: 403, statusMessage: 'No access', message: 'No access to this app', url: '' } as MicroMError;

        const menu_items = result[0].records.map((record) => `${record[0]}_${record[1]}`);

        await this.#saveEnabledMenus(menu_items);

        this.#ENABLED_MENUS = new Set<string>(menu_items);
    }

    async #saveEnabledMenus(data: string[]) {
        await this.#DATA_STORAGE.saveData(this.#APP_ID, ENABLED_MENUS_DATA_KEY, data);
    }

    async #readEnabledMenus(): Promise<string[] | null> {
        return await this.#DATA_STORAGE.readData(this.#APP_ID, ENABLED_MENUS_DATA_KEY);
    }

    async #deleteEnabledMenus() {
        await this.#DATA_STORAGE.deleteData(this.#APP_ID, ENABLED_MENUS_DATA_KEY);
    }

    //*** File upload/download
    getDocumentURL(fileGuid: string): string {
        return fileGuid ? `${this.#API_URL}/${this.#APP_ID}/serve/${fileGuid}` : '';
    }

    getThumbnailURL(fileGuid: string, max_size: number = 150, quality: number = 75): string {
        return fileGuid ? `${this.#API_URL}/${this.#APP_ID}/thumbnail/${fileGuid}/${max_size}/${quality}` : '';
    }

    async upload(file: File, fileprocess_id: string, abort_signal: AbortSignal | null = null, max_size: number = 150, quality: number = 75, onProgress: FetchUploadProgress = () => { }): Promise<FileUploadResponse> {
        return this.#uploadFile(file, fileprocess_id, abort_signal, max_size, quality, onProgress);
    }

    async downloadBlob(fileUrl: string, abort_signal: AbortSignal | null = null): Promise<Blob> {

        await this.#checkAndRefreshToken();
        if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in` } as MicroMError; }

        const response = await fetch(fileUrl, {
            headers: { ...(this.#TOKEN ? { "Authorization": `Bearer ${this.#TOKEN.access_token}` } : {}) },
            mode: this.#REQUEST_MODE,
            cache: 'no-store',
            credentials: 'include',
            referrerPolicy: 'strict-origin-when-cross-origin',
            signal: abort_signal
        });

        if (!response.ok) {
            throw new Error(`Network error: ${response.status} - ${response.statusText}`);
        }

        return await response.blob();
    }

    async #uploadFile(file: File, fileprocess_id: string, abort_signal: AbortSignal | null = null, max_size: number = 150, quality: number = 75, onProgress: FetchUploadProgress = () => { }) {
        await this.#checkAndRefreshToken();
        if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in` } as MicroMError; }

        const url = new URL(`${this.#API_URL}/${this.#APP_ID}/tmpupload`);
        url.searchParams.set("file_name", file.name);
        url.searchParams.set("fileprocess_id", fileprocess_id);
        url.searchParams.set("maxSize", max_size.toFixed(0));
        url.searchParams.set("quality", quality.toFixed(0));

        const totalSize = file.size;
        let uploadedSize = 0;

        return new Promise<FileUploadResponse>((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            xhr.open('POST', url.toString(), true);
            xhr.setRequestHeader("Content-Type", 'application/octet-stream');
            xhr.setRequestHeader("Authorization", `Bearer ${this.#TOKEN!.access_token}`);
            xhr.withCredentials = true; // for sending credentials in cross-origin scenarios

            xhr.upload.onprogress = (event) => {
                if (event.lengthComputable) {
                    uploadedSize = event.loaded;
                    onProgress(file, Math.ceil((uploadedSize / totalSize) * 100));
                }
            };

            xhr.onload = async () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    const data: FileUploadResponse = JSON.parse(xhr.responseText);
                    if (!data.ErrorMessage) {
                        data.documentURL = `${this.#API_URL}/${this.#APP_ID}/serve/${data.FileGuid}`;
                        data.thumbnailURL = `${this.#API_URL}/${this.#APP_ID}/thumbnail/${data.FileGuid}/${max_size}/${quality}`;
                    }
                    resolve(data);
                } else {
                    reject({
                        status: xhr.status,
                        statusMessage: xhr.statusText,
                        message: xhr.statusText,
                        url: xhr.responseURL
                    } as MicroMError);
                }
            };

            xhr.onerror = () => {
                reject({
                    status: xhr.status,
                    statusMessage: xhr.statusText,
                    message: xhr.statusText,
                    url: xhr.responseURL
                } as MicroMError);
            };

            if (abort_signal) {
                abort_signal.addEventListener('abort', () => {
                    xhr.abort();
                    reject({ status: 0, statusMessage: 'Aborted', message: 'Request was aborted by user' } as MicroMError);
                });
            }

            xhr.send(file);
        });
    }

    //*** Entities API
    async get(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, abort_signal: AbortSignal | null = null): Promise<ValuesObject> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Get });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "get")) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, [], "get", abort_signal);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, [], "get", abort_signal);
    }

    async insert(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, recordsSelection: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Insert });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "insert")) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, recordsSelection, "insert", abort_signal);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, recordsSelection, "insert", abort_signal);
    }

    async update(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, recordsSelection: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Update });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "update")) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, recordsSelection, "update", abort_signal);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, recordsSelection, "update", abort_signal);
    }

    async delete(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject | null, recordsSelection: ValuesObject[] | null, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Delete });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "delete")) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, recordsSelection, "delete", abort_signal);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, recordsSelection, "delete", abort_signal);
    }

    async lookup(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, lookup_name: string | null = null, abort_signal: AbortSignal | null = null) {
        if (!lookup_name) {
            this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.DefaultLookup });
        }
        else {
            this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.CustomLookup, views: [lookup_name] });
        }

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "lookup")) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, [], "lookup", abort_signal, lookup_name);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, [], "lookup", abort_signal, lookup_name);
    }

    async viewtoexcel(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, view_name: string, abort_signal: AbortSignal | null = null): Promise<Blob> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Views, views: [view_name] });

        return this.#submitToAPIBlob(entity_name, parent_keys, values, [], "viewtoexcel", abort_signal, view_name);
    }

    async view(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, view_name: string, abort_signal: AbortSignal | null = null): Promise<DataResult[]> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Views, views: [view_name] });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "viewstream", view_name)) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, [], "viewstream", abort_signal, view_name);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, [], "viewstream", abort_signal, view_name);
    }

    async proc(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, recordsSelection: ValuesObject[] | null, proc_name: string, abort_signal: AbortSignal | null = null): Promise<DataResult[]> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Procs, procs: [proc_name] });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "procstream", proc_name)) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, recordsSelection, "procstream", abort_signal, proc_name);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, recordsSelection, "procstream", abort_signal, proc_name);
    }

    async proctoexcel(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, recordsSelection: ValuesObject[] | null, proc_name: string, abort_signal: AbortSignal | null = null): Promise<Blob> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Procs, procs: [proc_name] });

        return this.#submitToAPIBlob(entity_name, parent_keys, values, recordsSelection, "proctoexcel", abort_signal, proc_name);
    }

    async process(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, recordsSelection: ValuesObject[] | null, proc_name: string, abort_signal: AbortSignal | null = null): Promise<DBStatusResult> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Procs, procs: [proc_name] });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "process", proc_name)) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, recordsSelection, "process", abort_signal, proc_name);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, recordsSelection, "process", abort_signal, proc_name);
    }

    async action<TReturn>(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, action_name: string, abort_signal: AbortSignal | null = null): Promise<TReturn> {
        this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Actions, actions: [action_name] });

        if (!this.#TOKEN && this.#isPublicAPI(entity_name, "action", undefined, action_name)) {
            return this.#submitToPublicAPI(entity_name, parent_keys, values, [], "action", abort_signal, action_name);
        }
        return this.#submitToAPI(entity_name, parent_keys, values, [], "action", abort_signal, action_name);
    }

    async import(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject, import_procname: string | null, abort_signal: AbortSignal | null = null): Promise<ImpDataResult> {
        if (!import_procname) {
            this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Import });
        }
        else {
            this.#recordAccess({ entityName: entity_name, access: AllowedRouteFlags.Import, procs: [import_procname] });
        }

        return this.#submitToAPI(entity_name, parent_keys, values, [], "import", abort_signal, import_procname);
    }

    async #submitToAPIBlob(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject | null
        , recordsSelection: ValuesObject[] | null, action: APIAction, abort_signal: AbortSignal | null = null, additional_route: string | null = null) {

        const extra_route = (additional_route !== null) ? `/${additional_route}` : '';
        const route = `${this.#API_URL}/${this.#APP_ID}/ent/${entity_name}/${action}${extra_route}`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const body = JSON.stringify({ ParentKeys: parent_keys, Values: values, RecordsSelection: recordsSelection }, JSONDateWithTimezoneReplacer);
            const res = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: abort_signal,
                body: body
            });

            if (!res.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await res.text(); } catch { }
                throw { status: res?.status, statusMessage: res?.statusText, message: res?.statusText, url: res?.url, errorBody: error_body } as MicroMError;
            }

            const data = await res.blob();
            return data;

        }
        catch (error) {
            if (this.#REDIRECT_ON_401 && (error as MicroMError).status === 401) {
                console.warn(`${route} 401, redirecting to login page: ${this.#REDIRECT_ON_401}`);
                await this.localLogoff();
                window.location.href = this.#REDIRECT_ON_401;
                // return intentionally omitted
            }
            // MMC: this line should never execute if the redirect happens. 
            // In case the browser, webview or environment for any reason fails to redirect, we still throw the error to the caller.
            throw error;
        }
    }


    async #submitToAPI(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject | null
        , recordsSelection: ValuesObject[] | null, action: APIAction, abort_signal: AbortSignal | null = null, additional_route: string | null = null) {

        const extra_route = (additional_route !== null) ? `/${additional_route}` : '';
        const route = `${this.#API_URL}/${this.#APP_ID}/ent/${entity_name}/${action}${extra_route}`;

        try {
            await this.#checkAndRefreshToken();
            if (!this.#TOKEN) { throw { status: 401, statusMessage: `Can't execute request: Not logged in`, url: route } as MicroMError; }

            const body = JSON.stringify({ ParentKeys: parent_keys, Values: values, RecordsSelection: recordsSelection }, JSONDateWithTimezoneReplacer);
            const res = await fetch(route, {
                method: 'POST',
                headers: { "Content-Type": "application/json; charset=utf-8", "Authorization": `Bearer ${this.#TOKEN.access_token}` },
                mode: this.#REQUEST_MODE,
                cache: 'no-store',
                credentials: 'include',
                referrerPolicy: 'strict-origin-when-cross-origin',
                signal: abort_signal,
                body: body
            });

            if (!res.ok) {
                let error_body: string | undefined = undefined;
                try { error_body = await res.text(); } catch { }
                throw { status: res?.status, statusMessage: res?.statusText, message: res?.statusText, url: res?.url, errorBody: error_body } as MicroMError;
            }

            const data = await res.json();
            return data;

        }
        catch (error) {
            if (this.#REDIRECT_ON_401 && (error as MicroMError).status === 401) {
                console.warn(`${route} 401, redirecting to login page: ${this.#REDIRECT_ON_401}`);
                await this.localLogoff();
                window.location.href = this.#REDIRECT_ON_401;
                // return intentionally omitted
            }
            // MMC: this line should never execute if the redirect happens. 
            // In case the browser, webview or environment for any reason fails to redirect, we still throw the error to the caller.
            throw error;
        }
    }

    async #submitToPublicAPI(entity_name: string, parent_keys: ValuesObject | null, values: ValuesObject | null
        , recordsSelection: ValuesObject[] | null, action: APIAction, abort_signal: AbortSignal | null = null, additional_route: string | null = null) {
        const extra_route = (additional_route !== null) ? `/${additional_route}` : '';
        const route = `${this.#API_URL}/${this.#APP_ID}/public/${entity_name}/${action}${extra_route}`;

        const body = JSON.stringify({ ParentKeys: parent_keys, Values: values, RecordsSelection: recordsSelection }, JSONDateWithTimezoneReplacer);
        const res = await fetch(route, {
            method: 'POST',
            headers: { "Content-Type": "application/json; charset=utf-8" },
            mode: this.#REQUEST_MODE,
            cache: 'no-store',
            credentials: 'include',
            referrerPolicy: 'strict-origin-when-cross-origin',
            signal: abort_signal,
            body: body
        });

        if (!res.ok) {
            let error_body: string | undefined = undefined;
            try { error_body = await res.text(); } catch { }
            throw { status: res?.status, statusMessage: res?.statusText, message: res?.statusText, url: res?.url, errorBody: error_body } as MicroMError;
        }

        const data = await res.json();

        return data;
    }

    #isPublicAPI(entity_name: string, action: APIAction, proc_name?: string, action_name?: string): boolean {
        const endpoint = this.#publicEndpoints[entity_name];

        if (endpoint) {
            switch (action) {
                case "get": return (endpoint.AllowedAccess & 1 << 3) !== 0;
                case "insert": return (endpoint.AllowedAccess & 1 << 0) !== 0;
                case "update": return (endpoint.AllowedAccess & 1 << 1) !== 0;
                case "delete": return (endpoint.AllowedAccess & 1 << 2) !== 0;
                case "lookup": return (endpoint.AllowedAccess & 1 << 4) !== 0;
                case "action": return (endpoint.AllowedActions && action_name && endpoint.AllowedActions.has(action_name)) || false;

                case "view": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;
                case "viewstream": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;
                case "viewtoexcel": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;

                case "proc": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;
                case "process": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;
                case "procstream": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;
                case "proctoexcel": return (endpoint.AllowedProcs && proc_name && endpoint.AllowedProcs.has(proc_name)) || false;

                default: return false;
            }
        }
        return false;
    }


}
