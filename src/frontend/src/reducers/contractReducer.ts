import {
    CollectionAction,
    ReadonlyCollection,
    createCollectionRootReducer,
    createCollectionReducer,
    createEmptyCollection,
} from './utils';
import Personnel from '../models/Personnel';
import PersonnelRequest from '../models/PersonnelRequest';
import { Position, BasePosition } from '@equinor/fusion';

export type ContractState = {
    personnel: ReadonlyCollection<Personnel>;
    activeRequests: ReadonlyCollection<PersonnelRequest>;
    actualMpp: ReadonlyCollection<Position>;
    completedRequests: ReadonlyCollection<PersonnelRequest>;
};

const personnelReducer = createCollectionReducer<ContractState, 'personnel'>(
    (x, y) => x.personnelId === y.personnelId
);

const activeRequestsReducer = createCollectionReducer<ContractState, 'activeRequests'>(
    (x, y) => x.id === y.id
);

const completedRequestsReducer = createCollectionReducer<ContractState, 'completedRequests'>(
    (x, y) => x.id === y.id
);
const actualMppReducer = createCollectionReducer<ContractState, 'actualMpp'>(
    (x, y) => x.id === y.id
);

export const contractReducer = createCollectionRootReducer(
    <T extends keyof ContractState>(
        state: ContractState,
        action: CollectionAction<ContractState, T>
    ): ContractState => {
        switch (action.collection) {
            case 'personnel':
                return personnelReducer(
                    state,
                    action as CollectionAction<ContractState, 'personnel'>
                );

            case 'activeRequests':
                return activeRequestsReducer(
                    state,
                    action as CollectionAction<ContractState, 'activeRequests'>
                );

            case 'actualMpp':
                return actualMppReducer(
                    state,
                    action as CollectionAction<ContractState, 'actualMpp'>
                );
            case 'completedRequests':
                return completedRequestsReducer(
                    state,
                    action as CollectionAction<ContractState, 'completedRequests'>
                );
        }

        return state;
    }
);

export const createInitialState = (): ContractState => ({
    personnel: createEmptyCollection<Personnel>(),
    activeRequests: createEmptyCollection<PersonnelRequest>(),
    actualMpp: createEmptyCollection<Position>(),
    completedRequests: createEmptyCollection<PersonnelRequest>(),
});
