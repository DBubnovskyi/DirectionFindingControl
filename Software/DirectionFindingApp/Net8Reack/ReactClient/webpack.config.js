const path = require('path');
const fs = require('fs');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const webpack = require('webpack');

const secondOutputPath = process.env.SECOND_OUTPUT_PATH || '../BlazorRotatorServer/wwwroot/';

class CopyStaticAssetsPlugin {
  apply(compiler) {
    compiler.hooks.afterEmit.tap('CopyStaticAssetsPlugin', () => {
      const sourceAssetsDir = path.resolve(__dirname, 'src', 'assets');
      const outputDir = compiler.options.output.path;
      const targetAssetsDir = path.join(outputDir, 'assets');

      if (!fs.existsSync(sourceAssetsDir)) {
        return;
      }

      fs.mkdirSync(targetAssetsDir, { recursive: true });
      fs.cpSync(sourceAssetsDir, targetAssetsDir, { recursive: true, force: true });
    });
  }
}

class CopyToSecondOutputPlugin {
  apply(compiler) {
    compiler.hooks.afterEmit.tap('CopyToSecondOutputPlugin', () => {
      if (!secondOutputPath || !secondOutputPath.trim()) {
        return;
      }

      const sourceDir = compiler.options.output.path;
      const targetDir = path.resolve(__dirname, secondOutputPath.trim());

      fs.mkdirSync(targetDir, { recursive: true });

      const sourceEntries = fs.readdirSync(sourceDir);
      sourceEntries.forEach((entryName) => {
        const sourceEntryPath = path.join(sourceDir, entryName);
        const targetEntryPath = path.join(targetDir, entryName);
        fs.cpSync(sourceEntryPath, targetEntryPath, { recursive: true, force: true });
      });

      console.log(`[build] Copied artifacts to second output: ${targetDir}`);
    });
  }
}

module.exports = {
  mode: 'development',
  entry: './src/index.jsx',
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: 'bundle.js',
  },
  target: 'web',
  module: {
    rules: [
      {
        test: /\.(js|jsx)$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: {
            presets: ['@babel/preset-react']
          }
        }
      },
      {
        test: /\.css$/,
        use: ['style-loader', 'css-loader']
      },
      {
        test: /\.(scss|sass)$/,
        use: ['style-loader', 'css-loader', 'sass-loader']
      }
    ]
  },
  resolve: {
    extensions: ['.js', '.jsx'],
    fallback: {
      "path": false,
      "fs": false,
      "crypto": false,
      "process": require.resolve("process/browser.js")
    },
    alias: {
      'process/browser': require.resolve('process/browser.js')
    }
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: './src/index.html',
      favicon: './src/assets/icons/favicon.svg'
    }),
    new webpack.ProvidePlugin({
      process: 'process/browser',
      Buffer: ['buffer', 'Buffer']
    }),
    new CopyStaticAssetsPlugin(),
    new CopyToSecondOutputPlugin()
  ],
  devServer: {
    static: {
      directory: path.join(__dirname, 'dist'),
    },
    compress: true,
    port: 8080,
    hot: true,
    proxy: [
      {
        context: ['/api/rotator'],
        target: 'https://localhost:7070',
        changeOrigin: true,
        secure: false,
      }
    ]
  },
  devtool: 'source-map'
};
