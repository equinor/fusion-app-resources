
export type PersonnelDiscipline = {
  name : string
}

type Personnel = {
  personnelId:string,
  azureUniquePersonId? : string,
  name:string,
  jobTitle:string,
  phoneNumber:string,
  mail:string,
  azureAdStatus: "Available"|"Invited"|"NoAccount",
  hasCV : boolean,
  disciplines : PersonnelDiscipline[],
};

export default Personnel;
