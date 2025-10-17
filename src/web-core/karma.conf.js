module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: {
      jasmine: {},
      clearContext: false
    },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage'),
      subdir: '.',
      reporters: [
        { type: 'html' }, 
        { type: 'text-summary' },
        { type: 'cobertura', file: 'cobertura-coverage.xml' },
        { type: 'lcovonly', file: 'lcov.info' }
      ]
    },
    reporters: ['progress', 'kjhtml', 'coverage'],
    browsers: ['ChromeHeadless'],
    restartOnFileChange: true
  });
};
