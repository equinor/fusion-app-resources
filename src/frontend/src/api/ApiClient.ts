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
import Person from '../models/Person';
import CreatePersonnelRequest from '../models/CreatePersonnelRequest';

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

    async getContractAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.contract(projectId, contractId);
        const response = await this.httpClient.getAsync<Contract, FusionApiHttpErrorResponse>(url);
        return response.data;
    }

    async getAvailableContractsAsync(projectId: string) {
        const url = this.resourceCollection.availableContracts(projectId);
        const response = await this.httpClient.getAsync<
            ApiCollection<AvailableContract>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data.value;
    }

    async getPersonnelAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.personnel(projectId, contractId);
        const response = await this.httpClient.getAsync<
            ApiCollection<Personnel>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data.value;
    }

    async createPersonnelAsync(projectId: string, contractId: string, personnel: Personnel) {
        const url = this.resourceCollection.personnel(projectId, contractId);
        const reponse = await this.httpClient.postAsync<
            Personnel,
            Personnel,
            FusionApiHttpErrorResponse
        >(url, personnel);
        return reponse.data;
    }

    async createPersonnelCollectionAsync(
        projectId: string,
        contractId: string,
        personnel: Personnel[]
    ) {
        const url = this.resourceCollection.personnelCollection(projectId, contractId);
        const reponse = await this.httpClient.postAsync<
            Personnel[],
            Personnel[],
            FusionApiHttpErrorResponse
        >(url, personnel);
        return reponse.data;
    }

    async updatePersonnelAsync(projectId: string, contractId: string, personnel: Personnel) {
        const url = this.resourceCollection.personnel(projectId, contractId, personnel.personnelId);
        const reponse = await this.httpClient.putAsync<
            Personnel,
            Personnel,
            FusionApiHttpErrorResponse
        >(url, personnel);
        return reponse.data;
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

    async createExternalCompanyRepresentativeAsync(
        projectId: string,
        contractId: string,
        request: CreatePositionRequest
    ) {
        const url = combineUrls(
            this.resourceCollection.contract(projectId, contractId),
            'external-company-representative'
        );

        const response = await this.httpClient.putAsync<
            CreatePositionRequest,
            Position,
            FusionApiHttpErrorResponse
        >(url, request);

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

        const response = await this.httpClient.putAsync<
            CreatePositionRequest,
            Position,
            FusionApiHttpErrorResponse
        >(url, request);

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
        return response.data.value;
    }

    async createPersonnelRequestAsync(
        projectId: string,
        contractId: string,
        request: CreatePersonnelRequest
    ) {
        const url = this.resourceCollection.personnelRequests(projectId, contractId);
        const reponse = await this.httpClient.postAsync<
            CreatePersonnelRequest,
            PersonnelRequest,
            FusionApiHttpErrorResponse
        >(url, request);
        return reponse.data;
    }

    async updatePersonnelRequestAsync(
        projectId: string,
        contractId: string,
        requestId: string,
        request: CreatePersonnelRequest
    ) {
        const url = this.resourceCollection.personnelRequest(projectId, contractId, requestId);
        const response = await this.httpClient.putAsync<
            CreatePersonnelRequest,
            PersonnelRequest,
            FusionApiHttpErrorResponse
        >(url, request);
        return response.data;
    }
}
