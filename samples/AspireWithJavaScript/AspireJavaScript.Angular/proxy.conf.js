module.exports = {
    '/api': {
      target: process.env['services__weatherapi__1'] || 'http://localhost:5084',
      pathRewrite: {
        '^/api': '',
      },
    },
  };
