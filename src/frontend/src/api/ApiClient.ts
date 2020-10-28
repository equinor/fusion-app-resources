import {
    IHttpClient,
    FusionApiHttpErrorResponse,
    combineUrls,
    Position,
    HttpClientParseError,
} from '@equinor/fusion';
import ResourceCollection from './ResourceCollection';
import Personnel from '../models/Personnel';
import Contract from '../models/contract';
import ApiCollection, { ApiCollectionRequest } from '../models/apiCollection';
import AvailableContract from '../models/availableContract';
import CreatePositionRequest from '../models/createPositionRequest';
import PersonnelRequest from '../models/PersonnelRequest';
import CreatePersonnelRequest from '../models/CreatePersonnelRequest';
import PersonDelegation, {
    PersonDelegationClassification,
    PersonDelegationRequest,
    ReCertifyPersonDelegationRequest,
} from '../models/PersonDelegation';
import { formatDateToYDMString } from './utils';
import ExcelParseReponse from '../models/ExcelParseResponse';
import ReadableStreamResponse from '../models/ReadableStreamResponse';

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

    async getPersonnelWithPositionsAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.personnel(
            projectId,
            contractId,
            undefined,
            'positions'
        );
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
            ApiCollectionRequest<Personnel>[],
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

    async deletePersonnelAsync(projectId: string, contractId: string, personnel: Personnel) {
        const url = this.resourceCollection.personnel(projectId, contractId, personnel.personnelId);
        const responseParser = async (response: Response) => {
            try {
                if ([200, 204].includes(response.status)) return personnel;

                throw reponse;
            } catch (parseError) {
                throw new HttpClientParseError(parseError);
            }
        };

        const reponse = await this.httpClient.deleteAsync<Personnel, Personnel>(
            url,
            null,
            responseParser
        );

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
        filter?: 'active' | 'completed'
    ) {
        const filterCondition = filter
            ? filter === 'active'
                ? 'state eq Created or state eq SubmittedToCompany'
                : "state eq 'ApprovedByCompany' or state eq 'RejectedByContractor' or state eq 'RejectedByCompany'"
            : undefined;
        const url = this.resourceCollection.personnelRequests(
            projectId,
            contractId,
            filterCondition
        );
        const response = await this.httpClient.getAsync<
            ApiCollection<PersonnelRequest>,
            FusionApiHttpErrorResponse
        >(url);
        return response.data.value;
    }
    async getPersonnelRequestAsync(projectId: string, contractId: string, requestId: string) {
        const url = this.resourceCollection.personnelRequest(projectId, contractId, requestId);
        const response = await this.httpClient.getAsync<
            PersonnelRequest,
            FusionApiHttpErrorResponse
        >(url);
        return response.data;
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

    async approveRequestAsync(projectId: string, contractId: string, requestId: string) {
        const url = this.resourceCollection.approvePersonnelRequest(
            projectId,
            contractId,
            requestId
        );
        const response = await this.httpClient.postAsync<
            void,
            PersonnelRequest,
            FusionApiHttpErrorResponse
        >(url, undefined);
        return response.data;
    }
    async rejectRequestAsync(
        projectId: string,
        contractId: string,
        requestId: string,
        reason: string
    ) {
        const url = this.resourceCollection.rejectPersonnelRequest(
            projectId,
            contractId,
            requestId
        );

        type RejectRequest = {
            reason: string;
        };

        const response = await this.httpClient.postAsync<
            RejectRequest,
            PersonnelRequest,
            FusionApiHttpErrorResponse
        >(url, {
            reason,
        });
        return response.data;
    }

    public async deleteRequestAsync(projectId: string, contractId: string, requestId: string) {
        const url = this.resourceCollection.personnelRequest(projectId, contractId, requestId);
        const response = await this.httpClient.deleteAsync<void, FusionApiHttpErrorResponse>(
            url,
            {},
            () => Promise.resolve()
        );
        return response.data;
    }

    public async canEditActionAsync(
        projectId: string,
        contractId: string,
        requestId: string,
        actionName: string
    ) {
        const url = this.resourceCollection.requestAction(
            projectId,
            contractId,
            requestId,
            actionName
        );

        try {
            const response = await this.httpClient.optionsAsync<void, FusionApiHttpErrorResponse>(
                url,
                {},
                () => Promise.resolve()
            );
            const allowHeader = response.headers.get('Allow');
            if (allowHeader !== null && allowHeader.indexOf('POST') !== -1) {
                return true;
            }
            return false;
        } catch (e) {
            return false;
        }
    }
    public async getPersonDelegationsAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.delegateRoles(projectId, contractId);
        const response = await this.httpClient.getAsync<
            PersonDelegation[],
            FusionApiHttpErrorResponse
        >(url);
        return response.data;
    }

    public async getDelegationAccessHeaderAsync(
        projectId: string,
        contractId: string,
        accountType: PersonDelegationClassification
    ) {
        const queryString = `?classification=${accountType}`;
        const url = this.resourceCollection.delegateRoles(projectId, contractId, queryString);

        try {
            const response = await this.httpClient.optionsAsync<void, FusionApiHttpErrorResponse>(
                url,
                {},
                () => Promise.resolve()
            );
            const allowHeader = response.headers.get('Allow');
            return allowHeader || '';
        } catch (e) {
            return '';
        }
    }

    public async createPersonRoleDelegationAsync(
        projectId: string,
        contractId: string,
        personRequest: PersonDelegationRequest
    ) {
        const url = this.resourceCollection.delegateRoles(projectId, contractId);
        const response = await this.httpClient.postAsync<
            PersonDelegationRequest,
            PersonDelegation,
            FusionApiHttpErrorResponse
        >(url, personRequest);
        return response.data;
    }

    public async deletePersonRoleDelegationAsync(
        projectId: string,
        contractId: string,
        roleId: string
    ) {
        const url = this.resourceCollection.delegateRole(projectId, contractId, roleId);
        const response = await this.httpClient.deleteAsync<void, FusionApiHttpErrorResponse>(
            url,
            {},
            () => Promise.resolve()
        );
        return response.data;
    }

    public async reCertifyRoleDelegationAsync(
        projectId: string,
        contractId: string,
        roleId: string,
        toDate: Date
    ) {
        const payload: ReCertifyPersonDelegationRequest = {
            validTo: formatDateToYDMString(toDate),
        };
        const url = this.resourceCollection.delegateRole(projectId, contractId, roleId);
        const response = await this.httpClient.patchAsync<
            ReCertifyPersonDelegationRequest,
            PersonDelegation,
            FusionApiHttpErrorResponse
        >(url, payload);
        return response.data;
    }

    public async parseExcelFileAsync(file: File) {
        const url = this.resourceCollection.parseExcelFile();
        const data = new FormData();
        data.append('File', file);
        const response = await this.httpClient.postFormAsync<
            ExcelParseReponse,
            FusionApiHttpErrorResponse
        >(url, data);
        return response.data;
    }

    public async getPersonnelExcelTemplate() {
        const url = this.resourceCollection.personnelExcelTemplate();
        const responseParser = (r: any) => r;
        const response = await this.httpClient.getAsync<
            ReadableStreamResponse,
            FusionApiHttpErrorResponse
        >(url, null, responseParser);

        return response.data;
    }

    public async deleteMppPositionAsync(projectId: string, contractId: string, positionId: string) {
        const url = this.resourceCollection.mppPosition(projectId, contractId, positionId);
        return await this.httpClient.deleteAsync<void, FusionApiHttpErrorResponse>(url, {}, () =>
            Promise.resolve()
        );
    }
    public async canDeleteMppPositionsAsync(projectId: string, contractId: string) {
        const url = this.resourceCollection.mppPositions(projectId, contractId);
        try {
            const response = await this.httpClient.optionsAsync<void, FusionApiHttpErrorResponse>(
                url,
                {},
                () => Promise.resolve()
            );

            const allowHeader = response.headers.get('Allow');
            if (allowHeader !== null && allowHeader.toLowerCase().indexOf('delete') !== -1) {
                return true;
            }

            return false;
        } catch (e) {
            return false;
        }
    }
}
