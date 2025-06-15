import globals from "globals";
import { parser as tsParser, configs as tsConfigs } from "typescript-eslint";
import pluginReact from "eslint-plugin-react";
import * as reactHooks from "eslint-plugin-react-hooks";
import { importX } from "eslint-plugin-import-x";
import { defineConfig } from "eslint/config";

export default defineConfig([
  {
    settings: {
      react: {
        version: "detect",
      },
    },
  },
  tsConfigs.recommended,
  pluginReact.configs.flat.recommended,
  pluginReact.configs.flat["jsx-runtime"],
  reactHooks.configs["recommended-latest"],
  importX.flatConfigs.recommended,
  importX.flatConfigs.typescript,
  {
    files: ["**/*.ts", "**/*.tsx"],
    languageOptions: {
      globals: globals.browser,
      parser: tsParser,
      parserOptions: {
        project: "./tsconfig.json",
        ecmaFeatures: {
          jsx: true,
        },
      },
    },
    rules: {
      "@typescript-eslint/no-floating-promises": "error",
      "@typescript-eslint/require-await": "error",
      "react-hooks/exhaustive-deps": "warn",
    },
  },
]);
