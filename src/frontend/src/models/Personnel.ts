export type PersonnelDiscipline = {
    name: string;
};

export type azureAdStatus = 'Available' | 'InviteSent' | 'NoAccount';

type Personnel = {
    personnelId: string;
    azureUniquePersonId?: string;
    name: string;
    firstName: string | null;
    lastName: string | null;
    jobTitle: string;
    phoneNumber: string;
    mail: string;
    azureAdStatus?: azureAdStatus
    hasCV?: boolean;
    disciplines: PersonnelDiscipline[];
    created?: Date | null;
    updated?: Date | null;
};

export default Personnel;
