import { combineUrls } from '@equinor/fusion';

export default class GithubApiClient {
    constructor(private baseUrl: string) {}

    async getContractManagementAsync() {
        const url = combineUrls(this.baseUrl, 'docs', 'contract-management.md');
        const response = await fetch(url);

        return response.text();
    }
}
