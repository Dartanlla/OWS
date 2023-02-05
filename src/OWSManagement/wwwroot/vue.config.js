const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
    transpileDependencies: true,
    devServer: {
        port: 5001,
        proxy: {
            '^/api': {
                target: 'http://localhost:5205'
            },
        }
    },
    publicPath: process.env.NODE_ENV === 'production'
        ? '/'
        : '/'
})
