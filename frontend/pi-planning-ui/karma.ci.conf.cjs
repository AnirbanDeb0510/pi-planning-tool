module.exports = function (config) {
  config.set({
    client: {
      clearContext: false,
    },
    reporters: ['progress'],
    logLevel: config.LOG_INFO,
    autoWatch: false,
    singleRun: true,
    restartOnFileChange: false,
    browsers: ['ChromeHeadlessNoSandbox'],
    customLaunchers: {
      ChromeHeadlessNoSandbox: {
        base: 'ChromeHeadless',
        flags: [
          '--no-sandbox',
          '--disable-setuid-sandbox',
          '--disable-dev-shm-usage',
          '--disable-gpu',
        ],
      },
    },
  });
};
