import { Parameters } from '@storybook/addons';
export declare const PARAM_KEY = "test";
export declare const ADDON_ID = "storybookjs/test";
export declare const PANEL_ID: string;
export declare const ADD_TESTS: string;
interface AddonParameters extends Parameters {
    jest?: string | string[] | {
        disabled: true;
    };
}
export declare function defineJestParameter(parameters: AddonParameters): string[] | null;
export {};
