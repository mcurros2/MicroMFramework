export interface TwoFactorLoginResult {
    requires_two_factor: true;
    two_factor_challenge_id: string;
    two_factor_provider?: string;
    two_factor_flow?: "authenticator" | "email_setup" | "email_recovery" | "support_required" | "sql_admin_setup" | "sql_admin_authenticator";
    two_factor_setup_required?: boolean;
    authenticator_management_required?: boolean;
    qr_code_data_url?: string;
    username: string;
    email?: string;
}
