import { FusionEnvironment } from '@equinor/fusion';

export const getResourceApiBaseUrl = (env: FusionEnvironment) => {
    switch (env.env) {
        case 'FQA':
            return 'https://resources-api.fqa.fusion-dev.net';

        case 'FPRD':
            return 'https://resources-api.fprd.fusion-dev.net';
    }

    return 'https://resources-api.ci.fusion-dev.net';
};

export const getFunctionsBaseUrl = (env: FusionEnvironment) => {
    switch (env.env) {
        case 'FQA':
            return 'https://pro-f-common-FQA.azurewebsites.net';

        case 'FPRD':
            return 'https://pro-f-common-FPRD.azurewebsites.net';
    }

    return 'https://pro-f-common-CI.azurewebsites.net';
};
