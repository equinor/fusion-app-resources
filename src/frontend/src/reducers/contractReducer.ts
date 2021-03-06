import {
    CollectionAction,
    ReadonlyCollection,
    createCollectionRootReducer,
    createCollectionReducer,
    createEmptyCollection,
} from './utils';
import Personnel from '../models/Personnel';
import PersonnelRequest from '../models/PersonnelRequest';
import { Position } from '@equinor/fusion';
import PersonDelegation from '../models/PersonDelegation';

export type ContractState = {
    personnel: ReadonlyCollection<Personnel>;
    activeRequests: ReadonlyCollection<PersonnelRequest>;
    actualMpp: ReadonlyCollection<Position>;
    completedRequests: ReadonlyCollection<PersonnelRequest>;
    administrators: ReadonlyCollection<PersonDelegation>;
};

const personnelReducer = createCollectionReducer<ContractState, 'personnel'>(
    (x, y) => x.personnelId === y.personnelId
);

const personnelRequestsReducer = createCollectionReducer<
    ContractState,
    'activeRequests' | 'completedRequests'
>((x, y) => x.id === y.id);

const actualMppReducer = createCollectionReducer<ContractState, 'actualMpp'>(
    (x, y) => x.id === y.id
);

const administratorsReducer = createCollectionReducer<ContractState, 'administrators'>(
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
                return personnelRequestsReducer(
                    state,
                    action as CollectionAction<ContractState, 'activeRequests'>
                );
            case 'completedRequests':
                return personnelRequestsReducer(
                    state,
                    action as CollectionAction<ContractState, 'completedRequests'>
                );
            case 'actualMpp':
                return actualMppReducer(
                    state,
                    action as CollectionAction<ContractState, 'actualMpp'>
                );
            case 'administrators':
                return administratorsReducer(
                    state,
                    action as CollectionAction<ContractState, 'administrators'>
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
    administrators: createEmptyCollection<PersonDelegation>(),
});
