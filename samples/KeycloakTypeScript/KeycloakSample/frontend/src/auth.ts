import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const keycloakUrl = __KEYCLOAK_URL__;
const realm = __KEYCLOAK_REALM__;
const clientId = __KEYCLOAK_CLIENT_ID__;

export const oidcConfig = {
  authority: `${keycloakUrl}/realms/${realm}`,
  client_id: clientId,
  redirect_uri: window.location.origin + '/',
  post_logout_redirect_uri: window.location.origin + '/',
  response_type: 'code',
  scope: 'openid profile email',
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  automaticSilentRenew: true,
};

let _userManager: UserManager | null = null;

export function getUserManager(): UserManager {
  if (!_userManager) {
    _userManager = new UserManager(oidcConfig);
  }
  return _userManager;
}
