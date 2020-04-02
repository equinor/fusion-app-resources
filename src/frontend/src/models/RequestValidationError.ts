import { FusionApiHttpErrorResponse } from '@equinor/fusion';

type ValidationErrors = { [key: string]: string[] };

type RequestValidationError = FusionApiHttpErrorResponse & {
    type?: string;
    title?: string;
    status?: number;
    traceId?: string;
    errors?: ValidationErrors;
};

export default RequestValidationError;
