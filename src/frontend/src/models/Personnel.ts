
export type PersonnelDiscipline = {
  name : string
}

type Personnel = {
<<<<<<< HEAD
  personnelId:string,
  azureUniquePersonId? : string,
=======
  azureUniquePersonId : string,
>>>>>>> master
  name:string,
  jobTitle:string,
  phoneNumber:string,
  mail:string,
  azureAdStatus: "Available"|"Invited"|"NoAccount",
  hasCV : boolean,
  disciplines : PersonnelDiscipline[],
};

export default Personnel;
