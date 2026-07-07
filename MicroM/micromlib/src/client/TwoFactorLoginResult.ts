export interface TwoFactorLoginResult {
    requires_two_factor: true;
    two_factor_challenge_id: string;
    two_factor_provider?: string;
    username: string;
    email?: string;
}
