namespace MicroM.Web.Authentication.SSO;

public record JwksCacheMetric(
    string jwks_uri,
    string? server_etag,
    string? local_etag,
    bool server_not_modified_last,
    bool was_refreshed_last,
    bool from_cache_last,
    string? sent_if_none_match_last,
    int keys_count,
    DateTimeOffset last_fetched_utc
);