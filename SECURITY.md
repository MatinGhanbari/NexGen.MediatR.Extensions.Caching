# Security Policy

## Supported versions

Security fixes are applied to the **latest released** lockstep package set
(core, Redis, Garnet, and EntityFramework).

| Version | Supported |
|---------|-----------|
| 2.x     | Yes       |
| 1.x     | No        |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report privately through [GitHub Security Advisories](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/security/advisories/new).

Include:

- Affected package name(s) and version
- Target framework (`net8.0` / `net9.0` / `net10.0`)
- Provider (Memory / Redis / Garnet) and whether EF auto-evict is enabled
- Description of the issue and potential impact
- Steps to reproduce or a proof of concept, if available

You should receive an acknowledgement within a few days. If a fix is warranted,
we will coordinate a patch release and credit the reporter unless you prefer to
remain anonymous.
