module.exports = {
  extends: ['@equinor/eslint-config-fusion', 'plugin:react/recommended', 'plugin:react-hooks/recommended'],
  settings: {
    react: {
      version: 'detect',
    },
  },
  rules: {
    // react 17 rules
    'react/jsx-uses-react': 'off',
    'react/react-in-jsx-scope': 'off',
    'react/function-component-definition': [
      'warn',
      {
        namedComponents: 'arrow-function',
      },
    ],
    'react/no-array-index-key': 'warn',
    'react/no-multi-comp': 'warn',
    'react/no-this-in-sfc': 'error',
    'react/prefer-read-only-props': 'warn',
  },
};
