using MicroM.Extensions;
using System.Buffers.Binary;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using static MicroM.Extensions.ActiveDirectoryExtensions;

namespace MicroM.ActiveDirectory;

public record ADUserRecord(
    string SamAccountName,
    string? WindowsDomainName,
    string PrincipalName,
    string? FirstName,
    string? LastName,
    string EmailAddress,
    string? EmployeeNumber,
    string? SID,
    bool isDomainAdmin,
    bool isBuiltInAdmin,
    List<string>? GroupSIDs
    );

public enum ADAuthenticationResult
{
    InvalidUserDomain,
    UserNotAnEmail,
    UserNotFound,
    InvalidCredentials,
    Authenticated
}

public static class ADAttributes
{
    public const string SamAccountName = "sAMAccountName";
    public const string UserPrincipalName = "userPrincipalName";
    public const string GivenName = "givenName";
    public const string Surname = "sn";
    public const string Mail = "mail";
    public const string EmployeeNumber = "employeeNumber";
    public const string MsDsPrincipalName = "msDS-PrincipalName";
    public const string objectSid = "objectSid";
    public const string tokenGroups = "tokenGroups";
}

public static class ADCore
{
    public static LdapConnection CreateServiceConnection(string server, string serviceUser, string servicePwd)
    {
        var credentials = new NetworkCredential(serviceUser, servicePwd);
        var directoryIdentifier = new LdapDirectoryIdentifier(server);

        var connection = new LdapConnection(directoryIdentifier, credentials, AuthType.Basic);

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.All;
        connection.Timeout = TimeSpan.FromSeconds(5);
        connection.AutoBind = true;

        return connection;
    }


    public static async Task<(ADUserRecord? user_record, ADAuthenticationResult verification_result)> AuthenticateUserByEmailAsync(
        string adServer,
        string serviceAccountUser,
        string serviceAccountPwd,
        string searchContainer,
        string inputEmail,
        string inputPassword,
        string userPrincipalDomain)
    {
        if (!inputEmail.EndsWith($"@{userPrincipalDomain}", StringComparison.OrdinalIgnoreCase)) return (null, ADAuthenticationResult.InvalidUserDomain);
        if (inputEmail.ToMailAddress() == null) return (null, ADAuthenticationResult.UserNotAnEmail);

        using var connection = CreateServiceConnection(adServer, serviceAccountUser, serviceAccountPwd);

        var account = await FindAccountByEmailAsync(connection, inputEmail, searchContainer);

        if (account == null) return (null, ADAuthenticationResult.UserNotFound);

        ADAuthenticationResult result = ADAuthenticationResult.InvalidCredentials;
        try
        {
            var userCredentials = new NetworkCredential(account.PrincipalName, inputPassword);

            connection.Bind(userCredentials);

            result = ADAuthenticationResult.Authenticated;
        }
        catch (LdapException ex) when (ex.ErrorCode == 49) // invalid credentials
        {

        }

        return (account, result);
    }

    public static async Task<ADUserRecord?> FindAccountByEmailAsync(LdapConnection connection, string email, string container)
    {
        string safeValue = EscapeLdapFilterValue(email);
        string filter = $"(&(objectClass=user)(|(userPrincipalName={safeValue})(proxyAddresses=SMTP:{safeValue})))";
        ADUserRecord? account = null;

        string[] attributesToRetrieve =
        [
            ADAttributes.SamAccountName,
            ADAttributes.UserPrincipalName,
            ADAttributes.GivenName,
            ADAttributes.Surname,
            ADAttributes.Mail,
            ADAttributes.EmployeeNumber,
            ADAttributes.MsDsPrincipalName,
            ADAttributes.objectSid
        ];

        var request = new SearchRequest(
            container,
            filter,
            SearchScope.Subtree,
            attributesToRetrieve
        );

        var response = (SearchResponse)await Task.Factory.FromAsync(
            (callback, state) => connection.BeginSendRequest(request, PartialResultProcessing.NoPartialResultSupport, callback, state),
            connection.EndSendRequest,
            null);

        if (response.Entries.Count > 0)
        {
            var entry = response.Entries[0];
            string? samAccountName = entry.GetADAttribute<string?>(ADAttributes.SamAccountName);
            string? principalName = entry.GetADAttribute<string?>(ADAttributes.UserPrincipalName);

            if (!samAccountName.IsNullOrEmpty() && !principalName.IsNullOrEmpty())
            {
                string? firstName = entry.GetADAttribute<string?>(ADAttributes.GivenName);
                string? lastName = entry.GetADAttribute<string?>(ADAttributes.Surname);
                string emailAddress = entry.GetADAttribute<string?>(ADAttributes.Mail) ?? email;
                string? employeeNumber = entry.GetADAttribute<string>(ADAttributes.EmployeeNumber);

                string? msDsPrincipalName = entry.GetADAttribute<string?>(ADAttributes.MsDsPrincipalName);
                string? windowsDomainName = null;

                if (!string.IsNullOrEmpty(msDsPrincipalName) && msDsPrincipalName.Contains('\\'))
                {
                    windowsDomainName = msDsPrincipalName.Split('\\')[0];
                }

                var sidBytes = entry.GetADAttribute<byte[]?>(ADAttributes.objectSid);
                var tokenGroups = entry.GetADAttributes<byte[]?>(ADAttributes.tokenGroups);

                bool isBuiltInAdmin = false;
                bool isDomainAdmin = false;

                List<string>? groupSIDs = null;
                if (tokenGroups != null)
                {
                    groupSIDs = new List<string>(tokenGroups.Length);
                    foreach (var groupSidBytes in tokenGroups)
                    {
                        string groupSid = ConvertSidToString(groupSidBytes);
                        if (!string.IsNullOrEmpty(groupSid))
                        {
                            // Administrators (Built-in)
                            if (groupSid == "S-1-5-32-544")
                            {
                                isBuiltInAdmin = true;
                            }
                            // Domain Admins (RID 512)
                            else if (groupSid.EndsWith("-512"))
                            {
                                isDomainAdmin = true;
                            }
                            groupSIDs.Add(groupSid);
                        }
                    }
                }

                account = new ADUserRecord(
                    samAccountName!,
                    windowsDomainName,
                    principalName!,
                    firstName,
                    lastName,
                    emailAddress,
                    employeeNumber,
                    sidBytes != null ? ConvertSidToString(sidBytes) : null,
                    isDomainAdmin,
                    isBuiltInAdmin,
                    groupSIDs
                );
            }
        }

        return account;
    }

    private static string ConvertSidToString(ReadOnlySpan<byte> sidBytes)
    {
        const int HeaderLength = 8;
        const int SubAuthorityLength = 4;

        if (sidBytes.Length < HeaderLength)
            return string.Empty;

        byte revision = sidBytes[0];
        byte subAuthorityCount = sidBytes[1];

        int expectedLength = HeaderLength + (subAuthorityCount * SubAuthorityLength);
        if (sidBytes.Length < expectedLength)
            return string.Empty;

        var sb = new StringBuilder(capacity: 12 + (subAuthorityCount * 11));

        sb.Append("S-").Append(revision);

        // Identifier Authority: 6 bytes big-endian
        ulong identifierAuthority = 0;
        for (int i = 2; i < HeaderLength; i++)
        {
            identifierAuthority = (identifierAuthority << 8) | sidBytes[i];
        }

        sb.Append('-').Append(identifierAuthority);

        for (int i = 0; i < subAuthorityCount; i++)
        {
            int offset = HeaderLength + (i * SubAuthorityLength);
            uint subAuthority = BinaryPrimitives.ReadUInt32LittleEndian(
                sidBytes.Slice(offset, SubAuthorityLength));

            sb.Append('-').Append(subAuthority);
        }

        return sb.ToString();
    }

    private static string EscapeLdapFilterValue(string value)
    {
        var sb = new StringBuilder(value.Length);

        foreach (char ch in value)
        {
            switch (ch)
            {
                case '\\': sb.Append(@"\5c"); break;
                case '*': sb.Append(@"\2a"); break;
                case '(': sb.Append(@"\28"); break;
                case ')': sb.Append(@"\29"); break;
                case '\0': sb.Append(@"\00"); break;
                default: sb.Append(ch); break;
            }
        }

        return sb.ToString();
    }
}