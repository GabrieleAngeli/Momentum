# Security Policy / Politica di Sicurezza

## Controls / Controlli
**English:**
- JWT tokens are signed with a rotatable key stored in Key Vault (placeholder implementation).
- OpenFeature drives feature flags and licensing via a Dapr binding provider.
- Timescale and Ignite access follows the principle of least privilege.
- gRPC plus HTTPS are mandatory in production environments.
- Secrets must never live in the repository; use CI/CD variables instead.

**Italiano:**
- I token JWT sono firmati con una chiave rotabile memorizzata in Key Vault (implementazione placeholder).
- OpenFeature gestisce feature flag e licensing tramite un provider Dapr binding.
- L'accesso a Timescale e Ignite segue il principio del minimo privilegio.
- gRPC e HTTPS sono obbligatori negli ambienti produttivi.
- I secret non devono mai essere nel repository; usa variabili di CI/CD.
