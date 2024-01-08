module.exports = {
    '/api': {
      target: process.env['services__weatherapi__1'],
      pathRewrite: {
        '^/api': '',
      },
    },
  };