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

    personnel(
        projectId: string,
        contractId: string,
        personnelId?: string,
        expand?: string
    ): string {
        const base = combineUrls(
            this.contract(projectId, contractId),
            'resources',
            'personnel',
            personnelId || ''
        );

        if (expand) {
            return `${base}?$expand=${expand}`;
        }

        return base;
    }

    personnelCollection(projectId: string, contractId: string): string {
        return combineUrls(
            this.contract(projectId, contractId),
            'resources',
            'personnel-collection'
        );
    }

    personnelRequests(projectId: string, contractId: string, filter?: string): string {
        const personnelRequestsFilter = filter ? `&$filter=${filter}` : '';
        return combineUrls(
            this.contract(projectId, contractId),
            'resources',
            'requests?$expand=originalPosition' + personnelRequestsFilter
        );
    }

    personnelRequest(projectId: string, contractId: string, requestId: string) {
        return combineUrls(
            this.contract(projectId, contractId),
            'resources',
            'requests',
            requestId
        );
    }

    approvePersonnelRequest(projectId: string, contractId: string, requestId: string) {
        return combineUrls(this.personnelRequest(projectId, contractId, requestId), 'approve');
    }

    rejectPersonnelRequest(projectId: string, contractId: string, requestId: string) {
        return combineUrls(this.personnelRequest(projectId, contractId, requestId), 'reject');
    }

    requestAction(projectId: string, contractId: string, requestId: string, actionName: string) {
        return combineUrls(
            this.personnelRequest(projectId, contractId, requestId),
            'actions',
            actionName
        );
    }

    delegateRoles(projectId: string, contractId: string, search?: string) {
       const url = combineUrls(this.contract(projectId, contractId), 'delegated-roles');
       if(!search) {
           return url
       }
       return url + search
    }

    delegateRole(projectId: string, contractId: string, roleId: string) {
        return combineUrls(this.delegateRoles(projectId, contractId), roleId)
    }
}
