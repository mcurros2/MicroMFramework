# Class: MicroM.Web.Services.Security.PublicEndpointsMiddleware

## Overview
Middleware that restricts access to endpoints decorated with PublicEndpointAttribute.

## Constructors
| Constructor | Description |
|:--|:--|
| PublicEndpointsMiddleware(RequestDelegate next, IMicroMAppConfiguration config) | Creates middleware with next delegate and app configuration. |

## Methods
| Method | Description |
|:--|:--|
| InvokeAsync(HttpContext context) | Blocks requests to public endpoints not allowed in configuration. |

## Remarks
Ensures only allowed routes are accessible without authentication.

## See Also
- [Namespace Documentation](index.md)
