var path = require("path");
var webpack = require("webpack");
const CopyWebpackPlugin = require('copy-webpack-plugin');
var fableUtils = require("fable-utils");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

var babelOptions = fableUtils.resolveBabelOptions({
    presets: [
        ["env", {
            "targets": {
                "browsers": ["last 2 versions"]
            },
            "modules": false
        }]
    ],
    plugins: ["transform-runtime"]
});


var isProduction = process.argv.indexOf("-p") >= 0;
var port = process.env.SUAVE_FABLE_PORT || "8085";
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = {
    mode: isProduction ? "production" : "development",
    devtool: isProduction ? undefined : "source-map",
    optimization: {
        minimize: false
    },
    entry: resolve('./Client.fsproj'),
    output: {
        path: resolve('../../deploy/Client'),
        filename: "bundle.js"
    },
    resolve: {
        symlinks: false,
        modules: [resolve("../../node_modules/")]
    },
    devServer: {
        proxy: {
            '/api/*': {
                target: 'http://localhost:' + port,
                changeOrigin: true
            },
            '/mirrorsharp': {
                target: 'ws://localhost:8085',
                ws: true
            }
        },
        contentBase: "./public",
        hot: true,
        inline: true
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: babelOptions,
                        define: isProduction ? [] : ["DEBUG"]
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: babelOptions
                },
            },
            {
                test: /\.s(a|c)ss$/,
                loader: [
                    'style-loader',
                    'css-loader',
                    'sass-loader'
                ]
            },
            {
                test: /\.css$/,
                loader: [
                    'style-loader',
                    'css-loader'
                ]
            },
            {
                test: /\.(eot|svg|ttf|woff|woff2)(\?v=\d+\.\d+\.\d+)?$/,
                use: "file-loader"
            }
        ]
    },
    plugins: [
        ...(isProduction ? [] : [new webpack.HotModuleReplacementPlugin()]),
        ...(isProduction ? [] : [new webpack.NamedModulesPlugin()]),
        new CopyWebpackPlugin([
            "public/"
        ]),
    ]
};
