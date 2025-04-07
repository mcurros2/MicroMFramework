
export interface MicroMClientClaimTypes {
    username: string,
    useremail?: string,
    userinitials?: string,
    [k: string]: unknown;
};

export class MicroMToken {
    expiration: string;
    expires_in: number;
    access_token: string;
    refresh_token: string;
    token_type: string;
    claims: Partial<MicroMClientClaimTypes>;

    constructor(access_token = '', expires_in = 0, refresh_token = '', token_type = '', claims = {}) {
        this.expires_in = expires_in;

        const d = new Date();
        d.setTime(d.getTime() + (expires_in * 1000));
        this.expiration = d.toISOString();


        this.access_token = access_token;
        this.expires_in = expires_in;
        this.refresh_token = refresh_token;
        this.token_type = token_type;
        this.claims = Object.fromEntries(Object.entries(claims).filter(([key, value]) => {
            const exclude = ['access_token', 'expires_in', 'refresh-token', 'token_type'];
            return !exclude.includes(key);
        }));
    }

    className() {
        return MicroMToken.name;
    }


};
