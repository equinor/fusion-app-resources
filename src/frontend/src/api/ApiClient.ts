import { IHttpClient, FusionApiHttpErrorResponse } from '@equinor/fusion';
import ResourceCollection from './ResourceCollection';
import Personnel from '../models/Personnel';
import Contract from '../models/contract';
import ApiCollection from '../models/apiCollection';
import AvailableContract from '../models/availableContract';

export default class ApiClient {
    protected httpClient: IHttpClient;
    protected resourceCollection: ResourceCollection;

    constructor(httpClient: IHttpClient, baseUrl: string) {
        this.httpClient = httpClient;
        this.resourceCollection = new ResourceCollection(baseUrl);
    }

    async getContractsAsync(projectId: string) {
        const url = this.resourceCollection.contracts(projectId);
        return this.httpClient.getAsync<ApiCollection<Contract>, FusionApiHttpErrorResponse>(url);
    }

    async getAvailableContractsAsync(projectId: string) {
        const url = this.resourceCollection.contracts(projectId);
        return this.httpClient.getAsync<ApiCollection<AvailableContract>, FusionApiHttpErrorResponse>(url);
    }

    async getPersonnelAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.personnel(projectId, contractId);
        return this.httpClient.getAsync<ApiCollection<Personnel>, FusionApiHttpErrorResponse>(url);
    }
}
