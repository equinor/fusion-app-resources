
type CreatePersonnelRequest = {
    id?: string;
    description: string;
    position: CreatePersonnelRequestPosition | null;
    person: {
        mail: string;
    };
};

type CreatePersonnelRequestPosition = {
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
