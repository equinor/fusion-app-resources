module.exports = {
  parser: "@typescript-eslint/parser",
  plugins: [
    '@typescript-eslint',
    'prettier',
  ],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:prettier/recommended'
  ],
  "rules": {
    "@typescript-eslint/no-unused-vars": [
      "error",
      // allow arguments prefixed with underscore
      { "argsIgnorePattern": "^_" }
    ]
  }
}