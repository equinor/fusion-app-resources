export type PersonnelDiscipline = {
    name: string;
};

type Personnel = {
    personnelId: string;
    azureUniquePersonId?: string;
    name: string;
    firstName: string | null;
    lastName: string | null;
    jobTitle: string;
    phoneNumber: string;
    mail: string;
    azureAdStatus: 'Available' | 'InviteSent' | 'NoAccount';
    hasCV: boolean;
    disciplines: PersonnelDiscipline[];
};

export default Personnel;
