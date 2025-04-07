
export enum EndpointAccess {
    None = 0,
    Create = 1 << 0,
    Update = 1 << 1,
    Delete = 1 << 2,
    Get = 1 << 3,
    Lookup = 1 << 4,
    Execute = 1 << 5,
}

export interface PublicEndpoint {
    EntityName: string,
    AllowedAccess: EndpointAccess,
    AllowedProcs?: Set<string>,
    AllowedActions?: Set<string>
}