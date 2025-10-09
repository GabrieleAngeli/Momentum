const { shareAll, withModuleFederationPlugin } = require('@angular-architects/module-federation/webpack');

module.exports = withModuleFederationPlugin({
  name: 'webcore',
  exposes: {
    './Dashboard': './src/app/components/dashboard/dashboard.component.ts'
  },
  shared: shareAll({ singleton: true, strictVersion: true, requiredVersion: 'auto' })
});
