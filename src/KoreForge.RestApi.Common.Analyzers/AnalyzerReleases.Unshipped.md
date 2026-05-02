; Unshipped analyzer release entries.

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
API001 | Architecture | Error | External layer types must be internal
API002 | Architecture | Error | External cannot reference Domain/Internal/Client
API003 | Architecture | Error | Domain cannot reference Internal/Client
API004 | Architecture | Error | Internal cannot reference External
API005 | HttpContract | Error | Refit methods must return Task<ApiResponse<T>>
API006 | HttpContract | Error | Refit methods require trailing CancellationToken
API007 | Architecture | Warning | HttpClient usage restricted to Client layer
