const { shareAll, withModuleFederationPlugin } = require('@angular-architects/module-federation/webpack');

module.exports = withModuleFederationPlugin({
  name: 'shell',
  remotes: {},
  exposes: {},
  shared: shareAll({ singleton: true, strictVersion: true, requiredVersion: 'auto' })
});
