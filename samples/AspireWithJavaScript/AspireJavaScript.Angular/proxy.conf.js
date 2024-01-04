module.exports = {
    '/api': {
      target: process.env['services__weatherApi__1'],
      pathRewrite: {
        '^/api': '',
      },
    },
  };