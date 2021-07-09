import { FusionEnvironment } from '@equinor/fusion';

export const getResourceApiBaseUrl = (env: FusionEnvironment) => {
    switch (env.env) {
        case 'FQA':
            return 'https://resources-api.fqa.fusion-dev.net';

        case 'FPRD':
            return 'https://fap-resources-api-fprd.azurewebsites.net';
    }

    return 'https://resources-api-pr.CI.fusion-dev.net';
};

export const getFunctionsBaseUrl = (env: FusionEnvironment) => {
    switch (env.env) {
        case 'FQA':
            return 'https://pro-f-utility-FQA.azurewebsites.net';

        case 'FPRD':
            return 'https://pro-f-utility-FPRD.azurewebsites.net';
    }

    return 'https://pro-f-utility-CI.azurewebsites.net';
};

export const getFusionAppId = () => {
    if('clientId' in window) {
        return (window as any)['clientId'];
    }

    return '5a842df8-3238-415d-b168-9f16a6a6031b';
};