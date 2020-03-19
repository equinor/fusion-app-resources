import { IHttpClient, combineUrls, FusionApiHttpErrorResponse } from '@equinor/fusion';

export type ServiceNowIncidentRequestMetadata = {
    url: string;
    currentApp: string;
    sessionId: string;
    browser: string;
    timeZoneOffset: number;
    localStorage: Record<string, any>;
    custom: any;
};

export type ServiceNowIncidentRequest = {
    description: string;
    shortDescription: string;
    metadata: ServiceNowIncidentRequestMetadata;
};

export type ServiceNowIncident = {
    number: string;
    state: string;
    description: string;
    shortDescription: string;
    active: boolean;
};

export default class ServiceNowApiClient {
    constructor(private httpClient: IHttpClient, private baseUrl: string) {}

    async getIncidentsAsync() {
        const url = combineUrls(this.baseUrl, 'api', 'service-now', 'incidents');
        const response = await this.httpClient.getAsync<
            ServiceNowIncident[],
            FusionApiHttpErrorResponse
        >(url);

        return response.data;
    }

    async createIncidentAsync(incident: ServiceNowIncidentRequest) {
        const url = combineUrls(this.baseUrl, 'api', 'service-now', 'incidents');
        const response = await this.httpClient.postAsync<
            ServiceNowIncidentRequest,
            ServiceNowIncident,
            FusionApiHttpErrorResponse
        >(url, incident);

        return response.data;
    }
}
