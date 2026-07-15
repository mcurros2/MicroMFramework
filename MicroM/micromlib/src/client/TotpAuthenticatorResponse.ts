export interface TotpAuthenticatorResponse {
    authenticator_id: string;
    authenticator_name: string;
}

export interface TotpAuthenticatorsResponse {
    authenticators: TotpAuthenticatorResponse[];
}
