> ⚠️ **This framework is in ALPHA state**  
> API and structure may change without notice.

# MicroM
MicroM is a lightweight framework to create SQL Database Centric Apps.

## Installation

### Backend (NuGet)
Install the NuGet package for the backend framework:

```bash
dotnet add package MicroM.Core --prerelease
```

> Make sure to enable **"Include prerelease"** if you're using Visual Studio.

### Frontend (NPM)
Install the NPM package for the frontend UI library:

```bash
npm install @mcurros2/microm@alpha
```

---

## Set Up

What you need:

* Visual Studio 2022  
* SQL Server (we recommend using the latest version)

Technologies used:
* .NET 8  
* React  
* Typescript  
* Parcel

---

## Password recovery with multiple frontends

Register each frontend in the application's `FrontendURLS` and use this link in the `RECOVERY` email template:

```text
{FRONTEND_ORIGIN}/recover?code={RECOVERY_CODE}
```

`{FRONTEND_ORIGIN}` is the requesting browser's origin (scheme, hostname, and port), validated against that application's frontends. A request from `https://customers.myapp.com` therefore links to `https://customers.myapp.com/recover?code=…`. The path remains editable in the template; frontend subdirectory paths are not included in the origin.

Recovery-email requests require a valid `Origin` header. Missing, malformed, and unregistered origins fail without generating a code or queuing email. Global CORS allowances do not authorize recovery destinations. HTTPS is required except for explicitly registered HTTP localhost/loopback development URLs. Non-browser callers must supply a registered origin explicitly.

Existing stored templates are preserved: update them explicitly to use the new tag. Existing `{RECOVERY_CODE}` links continue to render. The browser client request body remains unchanged. Custom C# implementations and callers must add `string frontend_origin` before `CancellationToken` in `IAuthenticator.SendPasswordRecoveryEmail`, and `string? origin` in `IAuthenticationService.HandleSendRecoveryEmail`.

## Solution Projects

* **MicroMCore**  
  C# – Backend framework

* **Web API**  
  C# – Skeleton for your own API

* **LibraryTest**  
  C# – Tests

* **micromlib**  
  React + TypeScript + Mantine – UI component library
