export enum ApiAccountStatus {
  Available ="Available",
  Invited = "Invited",
  NoAccount = "NoAccount"
}

export type PersonnelDiscipline = {
  name : string
}

type Personnel = {
  AzureUniquePersonId : string,
  Name:string,
  JobTitle:string,
  PhoneNumber:string,
  Mail:string,
  AzureAdStatus: ApiAccountStatus,
  HasCV : boolean,
  Disciplines : PersonnelDiscipline[],

};

export default Personnel;
