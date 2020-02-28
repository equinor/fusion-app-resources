export type BasePosition ={
    id: string;
    name:string;
}

export type TaskOwner = {
    positionId: string;
}


type Position = {
    id: string;
    externalId: string | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo:Date | null;
    basePosition?: BasePosition;
    taskOwner?: TaskOwner;

}

export default Position