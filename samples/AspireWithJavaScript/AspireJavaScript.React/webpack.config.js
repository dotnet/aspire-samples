const HTMLWebpackPlugin = require("html-webpack-plugin");
const webpack = require("webpack");

module.exports = (env) => {
  return {
    entry: "./src/index.js",
    devServer: {
      port: env.PORT || 4001,
      allowedHosts: "all",
    },
    output: {
      path: `${__dirname}/dist`,
      filename: "bundle.js",
    },
    plugins: [
      new HTMLWebpackPlugin({
        template: "./src/index.html",
        favicon: "./src/favicon.ico",
      }),
      new webpack.DefinePlugin({
        "variables": {
          "REACT_APP_WEATHER_API_HTTPS": JSON.stringify(
            process.env.REACT_APP_WEATHER_API_HTTPS
          ),
          "REACT_APP_WEATHER_API_HTTP": JSON.stringify(
            process.env.REACT_APP_WEATHER_API_HTTP
          ),
        }
      }),
    ],
    module: {
      rules: [
        {
          test: /\.js$/,
          exclude: /node_modules/,
          use: {
            loader: "babel-loader",
            options: {
              presets: [
                "@babel/preset-env",
                ["@babel/preset-react", { runtime: "automatic" }],
              ],
            },
          },
        },
        {
          test: /\.css$/,
          exclude: /node_modules/,
          use: ["style-loader", "css-loader"],
        },
      ],
    },
  };
};
