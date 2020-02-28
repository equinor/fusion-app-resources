
export type PersonnelDiscipline = {
  name : string
}

type Personnel = {
  AzureUniquePersonId : string,
  Name:string,
  JobTitle:string,
  PhoneNumber:string,
  Mail:string,
  AzureAdStatus: "Available"|"Invited"|"NoAccount",
  HasCV : boolean,
  Disciplines : PersonnelDiscipline[],
};

export default Personnel;
