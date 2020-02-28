import { combineUrls } from '@equinor/fusion';

export default class ResourceCollection {
    protected baseUrl: string;
    constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  getPersonnel(projectId:string,contractId:string): string {
    return combineUrls(this.baseUrl,`projects/${projectId}/contracts/${contractId}/resources/personnel`);
  }
}