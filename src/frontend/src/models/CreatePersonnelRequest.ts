
type CreatePersonnelRequest = {
    id?: string;
    description: string;
    position: PersonnelRequestPosition | null;
    person: {
        mail: string;
    };
};

type PersonnelRequestPosition = {
    id: string | null; 
    basePosition: {
        id: string
    } | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: number;
    obs: string;
    taskOwner: {
        id: string
    } | null
};

export default CreatePersonnelRequest;
