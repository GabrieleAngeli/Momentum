export interface RemoteModuleDescriptor {
  id: string;
  url: string;
  permissions: string[];
  flags: string[];
  semver?: string;
}

export interface UiManifestDto {
  remotes: RemoteModuleDescriptor[];
  shared: Record<string, string>;
}

export interface UserPrincipalDto {
  id: string;
  email: string;
  displayName: string;
  tenantId?: string | null;
  roles: string[];
  permissions: string[];
  claims: Record<string, string>;
}

export interface AuthMeResponse {
  user: UserPrincipalDto;
  isAuthenticated: boolean;
  requiresMfa: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
  mfaCode?: string;
}

export interface LoginResponse {
  me: AuthMeResponse;
  jwtToken?: string | null;
}

export interface FlagValue {
  key: string;
  type: string;
  value: unknown;
  scope: string;
  scopeReference?: string | null;
}

export type FlagsEnvelope = Record<string, FlagValue>;

export interface FlagsDelta {
  updated: Record<string, FlagValue>;
  removed: string[];
}

export interface I18nResourceDto {
  language: string;
  namespace: string;
  resources: Record<string, unknown>;
  etag?: string | null;
}

export interface MenuEntryDto {
  id: string;
  label: string;
  route: string;
  requiredFlags: string[];
  requiredPermissions: string[];
  children: MenuEntryDto[];
}
