import { CollectionAction, ReadonlyCollection, merge, createCollectionReducer } from './utils';
import Personnel from '../models/Personnel';

export type ContractState = {
    personnel: ReadonlyCollection<Personnel>;
};

const personnelReducer = (
    state: ContractState,
    action: CollectionAction<ContractState, 'personnel'>
) => {
    switch (action.verb) {
        case 'merge':
            return {
                ...state,
                personnel: {
                    isFetching: false,
                    error: null,
                    data: merge(
                        state.personnel.data,
                        (x, y) => x.personnelId === y.personnelId,
                        action.payload
                    ),
                },
            };
    }
    
    return state;
};

export const contractReducer = createCollectionReducer(
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
        }

        return state;
    }
);
