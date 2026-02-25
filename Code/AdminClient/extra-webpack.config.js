const rtlcss = require('rtlcss');
const autoprefixer = require('autoprefixer');

module.exports = {
  module: {
    rules: [
      {
        test: /styles-rtl.scss$/i,
        use: [
          {
            loader: 'postcss-loader',
            options: {
              postcssOptions: { plugins: [autoprefixer, rtlcss] }
            }
          },
          'sass-loader',
        ],
      },
    ],
  },
};
