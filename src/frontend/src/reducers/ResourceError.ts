export type AccessRequirement = {
    code: string;
    description: string;
    outcome: string;
    wasEvaluated: boolean;
};

export type ResourceResponse = {
    error: ResourceResponseError;
};

export type ResourceResponseError = {
    code: string;
    message: string;
    accessRequirements?: AccessRequirement[];
};

interface ResourceError extends Error {
    statusCode: number;
    response: ResourceResponse;
}

export default ResourceError;
