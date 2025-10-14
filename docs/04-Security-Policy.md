# Security Policy

## Controls
- JWT tokens are signed with a rotatable key stored in Key Vault (placeholder implementation).
- OpenFeature drives feature flags and licensing via a Dapr binding provider.
- Timescale and Ignite access follows the principle of least privilege.
- gRPC plus HTTPS are mandatory in production environments.
- Secrets must never live in the repository; use CI/CD variables instead.
- Modular monolith deployments inherit the same security controls as the distributed topology to maintain parity across environments.
