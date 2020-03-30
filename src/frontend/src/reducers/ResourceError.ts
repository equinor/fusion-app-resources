type AccessRequirement = {
    code: string;
    description: string;
    outcome: string;
    wasEvaluated: boolean;
};

type ResourceResponse = {
    error: ResourceResponseError;
};

type ResourceResponseError = {
    code: string;
    message: string;
    accessRequirement?: AccessRequirement[];
};

interface ResourceError extends Error {
    statusCode: number;
    response: ResourceResponse;
}

export default ResourceError;
