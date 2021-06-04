export type PersonnelDiscipline = {
    name: string;
};

export type BasePosition = {
    id: string;
    name: string;
    discipline: string;
    projectType: string;
};

export type PositionContract = {
    id: string;
    name: string;
    contractNumber: string;
    company: {
        id: string;
        name: string;
    };
};

export type PositionProject = {
    projectId: string;
    name: string;
    domainId: string;
    projectType: string;
};

export type Position = {
    positionId: string;
    instanceId: string;
    name: string;
    obs: string;
    externalPositionId: string;
    appliesFrom: Date;
    appliesTo: Date;
    workload: number;
    basePosition: BasePosition;
    project: PositionProject;
    contract: PositionContract;
};

export type RequestPosition = {
    id: string;
    externalId: string;
    name: string;
    appliesFrom: Date;
    appliesTo: Date;
    workload: number;
    basePosition: BasePosition & {
        wasResolved: true;
    };
    taskOwner: {
        positionId: Date;
    };
};

export type RequestContract = {
    id: Date;
    internalId: Date;
    contractNumber: string;
    name: string;
    company: {
        id: Date;
        identifier: string;
        name: string;
    };
};

export type RequestProject = {
    id: Date;
    internalId: Date;
    name: string;
    projectMasterId: Date;
};

export type Requests = {
    id: string;
    created: Date;
    updated: Date;
    state: number;
    position: RequestPosition;
    contract: RequestContract;
    project: RequestProject;
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
    dawinciCode?: string | null;
    linkedInProfile?: string | null;
    azureAdStatus?: azureAdStatus;
    hasCV?: boolean;
    disciplines: PersonnelDiscipline[];
    created?: Date | null;
    updated?: Date | null;
    positions?: Position[] | null;
    requests?: Request[] | null;
    preferredContactMail?: string | null;
};

export default Personnel;
