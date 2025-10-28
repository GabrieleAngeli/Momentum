# Security Policy

## Supported versions

Security fixes are applied to the `main` branch and backported to active release branches on a case-by-case basis.

## Reporting a vulnerability

1. Email the core maintainers at `security@momentum.example.com` with a concise proof of concept.
2. Include the affected commit SHA, configuration details, and reproduction steps.
3. Encrypt sensitive information using the PGP key published in `docs/security/pgp.asc` (to be provided).
4. Expect an acknowledgement within **2 business days** and a full triage report within **5 business days**.

Please do **not** file public GitHub issues for untriaged vulnerabilities.

## Coordinated disclosure

- Critical vulnerabilities will trigger an out-of-band release and advisory.
- High severity issues are addressed within the sprint when possible. When mitigation requires more time, the security workflow will track accepted risk via suppressions.
- Medium and low severity findings are triaged weekly. Accepted risks are documented in `docs/security/README.md`.

## Running security checks locally

Use `make security` to execute the same toolchain configured in `.github/workflows/security-pr.yml`. The command refreshes vulnerability databases, runs SAST, dependency, secret, container, IaC, and optional DAST scans.

## Exceptions and suppressions

All suppressions must include:

- The tool (Gitleaks, Semgrep, tfsec, etc.).
- The reason for suppression.
- The tracking issue or ticket ID.
- A review date (90 days by default).

Document each suppression in `docs/security/README.md` to keep the baseline auditable.
