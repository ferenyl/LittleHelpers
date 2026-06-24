const apiUrl = process.env['services__apiservice__http__0'] || 'http://localhost:5465';

module.exports = {
  '/api': {
    target: apiUrl,
    secure: false,
    changeOrigin: true,
    ws: true,
    pathRewrite: { '^/api': '' },
  },
};
