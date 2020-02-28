import { IHttpClient, FusionApiHttpErrorResponse } from '@equinor/fusion';
import ResourceCollection from './ResourceCollection';
import Personnel from '../models/Personnel';


export default class ApiClient  {
  protected httpClient:IHttpClient;
  protected resourceCollection : ResourceCollection

  constructor(httpClient: IHttpClient, baseUrl: string) {
    this.httpClient = httpClient;
    this.resourceCollection = new ResourceCollection(baseUrl);
  }

  async personnel(contractId:string) {
    const url = this.resourceCollection.getPersonnel(contractId);
    return await this.httpClient.getAsync<Personnel[], FusionApiHttpErrorResponse>(url);
  }
}
