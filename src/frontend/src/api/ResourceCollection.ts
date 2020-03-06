import { combineUrls } from '@equinor/fusion';

export default class ResourceCollection {
    protected baseUrl: string;
    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    project(projectId: string) {
        return combineUrls(this.baseUrl, 'projects', projectId);
    }

    contracts(projectId: string) {
        return combineUrls(this.project(projectId), 'contracts');
    }

    availableContracts(projectId: string) {
        return combineUrls(this.project(projectId), 'available-contracts');
    }

    contract(projectId: string, contractId: string) {
        return combineUrls(this.contracts(projectId), contractId);
    }

    personnel(projectId: string, contractId: string): string {
        return combineUrls(this.contract(projectId, contractId), 'resources', 'personnel');
    }

    personnelCollection(projectId: string, contractId: string): string {
        return combineUrls(this.contract(projectId, contractId), 'resources', 'personnel-collection');
    }

    personnelUpdate(projectId: string, contractId: string, personnelId: string): string {
        return combineUrls(this.contract(projectId, contractId), 'resources', 'personnel', personnelId);
    }

    personnelRequests(projectId: string, contractId: string, filter?: string): string {
        const personnelRequestsFilter = filter ? `?$filter=${filter}` : '';
        return combineUrls(
            this.contract(projectId, contractId),
            'resources',
            'requests' + personnelRequestsFilter
        );
    }
}
