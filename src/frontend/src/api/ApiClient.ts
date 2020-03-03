import {
    IHttpClient,
    FusionApiHttpErrorResponse,
    combineUrls,
    Position,
    BasePosition,
} from '@equinor/fusion';
import ResourceCollection from './ResourceCollection';
import Personnel from '../models/Personnel';
import Contract from '../models/contract';
import ApiCollection from '../models/apiCollection';
import AvailableContract from '../models/availableContract';
import CreatePositionRequest from '../models/createPositionRequest';
import PersonnelRequest from '../models/PersonnelRequest';

export default class ApiClient {
    protected httpClient: IHttpClient;
    protected resourceCollection: ResourceCollection;

    constructor(httpClient: IHttpClient, baseUrl: string) {
        this.httpClient = httpClient;
        this.resourceCollection = new ResourceCollection(baseUrl);
    }

    async getContractsAsync(projectId: string) {
        const url = this.resourceCollection.contracts(projectId);
        const response = await this.httpClient.getAsync<
            ApiCollection<Contract>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data.value;
    }

    async getAvailableContractsAsync(projectId: string) {
        const url = this.resourceCollection.contracts(projectId);
        const response = await this.httpClient.getAsync<
            ApiCollection<AvailableContract>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data.value;
    }

    async getPersonnelAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.personnel(projectId, contractId);
        return this.httpClient.getAsync<ApiCollection<Personnel>, FusionApiHttpErrorResponse>(url);
    }

    async createContractAsync(projectId: string, contract: Contract) {
        const url = this.resourceCollection.contracts(projectId);
        const response = await this.httpClient.postAsync<
            Contract,
            Contract,
            FusionApiHttpErrorResponse
        >(url, contract);
        return response.data;
    }

    async updateContractAsync(projectId: string, contractId: string, contract: Contract) {
        const url = this.resourceCollection.contract(projectId, contractId);
        const response = await this.httpClient.putAsync<
            Contract,
            Contract,
            FusionApiHttpErrorResponse
        >(url, contract);

        return response.data;
    }

    async createExternalCompanyReprasentativeAsync(
        projectId: string,
        contractId: string,
        request: CreatePositionRequest
    ) {
        const url = combineUrls(
            this.resourceCollection.contract(projectId, contractId),
            'external-company-representative'
        );

        const response = await this.httpClient.postAsync<
            CreatePositionRequest,
            Position,
            FusionApiHttpErrorResponse
        >(url, request, null, () =>
            Promise.resolve({
                id: new Date().getTime().toString(),
                basePosition: request.basePosition as BasePosition,
                contractId,
                directChildCount: 0,
                externalId: new Date().getTime().toString(),
                instances: [],
                name: request.name,
                projectId,
                properties: {},
                totalChildCount: 0,
            })
        );

        return response.data;
    }

    async createExternalContractResponsibleAsync(
        projectId: string,
        contractId: string,
        request: CreatePositionRequest
    ) {
        const url = combineUrls(
            this.resourceCollection.contract(projectId, contractId),
            'external-contract-responsible'
        );

        const response = await this.httpClient.postAsync<
            CreatePositionRequest,
            Position,
            FusionApiHttpErrorResponse
        >(url, request, null, () =>
            Promise.resolve({
                id: new Date().getTime().toString(),
                basePosition: request.basePosition as BasePosition,
                contractId,
                directChildCount: 0,
                externalId: new Date().getTime().toString(),
                instances: [],
                name: request.name,
                projectId,
                properties: {},
                totalChildCount: 0,
            })
        );

        return response.data;
    }

    async getPersonnelRequestsAsync(
        projectId: string,
        contractId: string,
        filterOnActive?: boolean
    ) {
        const filter = filterOnActive ? 'isActive eq true' : undefined;
        const url = this.resourceCollection.personnelRequests(projectId, contractId, filter);
        const response = await this.httpClient.getAsync<
            ApiCollection<PersonnelRequest>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data;
    }
}
