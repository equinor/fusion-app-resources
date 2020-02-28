import { combineUrls } from '@equinor/fusion';

export default class ResourceCollection {
    protected baseUrl: string;
    constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  getPersonnel(contractId:string): string {
    return combineUrls(this.baseUrl,`api/${contractId}`);
  }

}