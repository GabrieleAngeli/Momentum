# Security Testing Playbook

The `security-pr.yml` workflow executes a multilayer security gate on every pull request.
Use this guide to reproduce the same checks locally and to manage suppressions.

## Local execution

A convenience wrapper is available via `make security` (see the repository `Makefile`).
It runs the script `tools/security/run-all.sh`, which orchestrates the following steps:

1. Refresh security databases (Trivy, Semgrep, Dependency-Check, OSV, Nuclei).
2. Build/test the application (.NET, Java, Node) and collect dependency advisories.
3. Run static analysis (Semgrep), secrets scanning (Gitleaks), and filesystem scans (Trivy).
4. Generate SBOMs with Syft and scan them with Grype and OSV-Scanner.
5. Evaluate Dockerfiles/images, IaC (Terraform, Ansible) and Kubernetes manifests with the same tooling used in CI.
6. Optionally execute DAST checks via OWASP ZAP and Nuclei against a preview endpoint by exporting `PREVIEW_BASE_URL` before invoking the script.

The script produces reports in `./reports` and summarises results in `./reports/security-summary.md`.
All commands are safe to re-run; caches live under `~/.cache`, `~/.nuget`, and `~/.m2`.

## Managing suppressions

- **Secrets (`.gitleaks.toml`):** Update the allowlist to silence intentional secrets. Each entry must link to a work item explaining why the credential is acceptable.
- **Semgrep (`.semgrepignore`):** Ignore generated folders only. Avoid global exclusions without peer review.
- **Dependencies (`dependency-check-suppression.xml`):** Record per-CVE suppressions with a tracking issue and expiry.
- **tfsec (`tfsec.yaml`) and Checkov (`.checkov.yml`):** Prefer inline `#tfsec:ignore`/`skip` annotations; fall back to these files for repository-wide suppressions.
- **Nuclei allowlist (`docs/security/nuclei-allowlist.yaml`):** Use this file to skip noisy templates for specific preview hosts.

Whenever you update suppressions, document the change and its approval in this file under a new subsection.

## Updating tool databases manually

- **Trivy:** `trivy --download-db-only`
- **Semgrep:** `semgrep --update`
- **Nuclei:** `nuclei -update-templates`
- **Dependency-Check:** `dependency-check.sh --updateonly`
- **OSV-Scanner:** `osv-scanner --lockfile --recursive .`

Keep the local caches warm by running the update script at least once per day when actively triaging security issues.

## Triage policy

1. **Critical findings:** Block the pull request immediately. File an incident and remediate before merging.
2. **High severity:** Allowed up to the configured threshold (default: 3). If exceeded, the PR is blocked until fixes or approved suppressions are in place.
3. **Medium / Low:** Review within two business days; create follow-up tickets when remediation cannot happen within the PR.
4. **DAST:** Findings coming from ZAP/Nuclei must be verified manually before suppression.

Record triage decisions in the PR conversation and reference them in future suppressions.
