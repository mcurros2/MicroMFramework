# Class: MicroM.Web.Middleware.DebugRoutesMiddleware

## Overview
Middleware that returns registered routes as JSON for diagnostics.

## Constructors
| Constructor | Description |
|:--|:--|
| DebugRoutesMiddleware(RequestDelegate next, string debugRoutesURL) | Initializes with next delegate and debug URL. |

## Methods
| Method | Description |
|:--|:--|
| Invoke(HttpContext context, IActionDescriptorCollectionProvider? provider) | Handles requests to the debug route and outputs route info. |

## Remarks
Returns route metadata when the request path matches the configured debug URL.

## See Also
- [Namespace Documentation](index.md)
