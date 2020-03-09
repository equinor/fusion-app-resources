import Contract from '../models/contract';
import { Position } from '@equinor/fusion';
import { merge, ReadonlyCollection, CollectionAction, createCollectionReducer } from './utils';

export type AppState = {
    contracts: ReadonlyCollection<Contract>;
    positions: ReadonlyCollection<Position>;
};

const extractPositionsFromContracts = (contracts: readonly Contract[]): Position[] => {
    return contracts
        .reduce<Position[]>(
            (p, c) => [
                ...p,
                c.companyRep as Position,
                c.contractResponsible as Position,
                c.externalCompanyRep as Position,
                c.externalContractResponsible as Position,
            ],
            []
        )
        .filter(p => p !== null);
};

const contractsReducer = (
    state: AppState,
    action: CollectionAction<AppState, 'contracts'>
): AppState => {
    switch (action.verb) {
        case 'merge':
            const stateWithMergedContracts = {
                ...state,
                contracts: {
                    isFetching: false,
                    error: null,
                    data: merge(state.contracts.data, (x, y) => x.id === y.id, action.payload),
                },
            };

            return appReducer(stateWithMergedContracts, {
                verb: 'merge',
                collection: 'positions',
                payload: extractPositionsFromContracts(stateWithMergedContracts.contracts.data),
            });
    }

    return state;
};

const positionsReducer = (
    state: AppState,
    action: CollectionAction<AppState, 'positions'>
): AppState => {
    switch (action.verb) {
        case 'merge':
            return {
                ...state,
                positions: {
                    isFetching: false,
                    error: null,
                    data: merge(state.positions.data, (x, y) => x.id === y.id, action.payload),
                },
            };
    }

    return state;
};

export const appReducer = createCollectionReducer(
    <T extends keyof AppState = keyof AppState>(
        state: AppState,
        action: CollectionAction<AppState, T>
    ): AppState => {
        switch (action.verb) {
            case 'fetch':
                return {
                    ...state,
                    [action.collection]: {
                        ...state[action.collection],
                        error: null,
                        isFetching: true,
                    },
                };

            case 'error':
                return {
                    ...state,
                    [action.collection]: {
                        ...state[action.collection],
                        isFetching: true,
                        error: action.error,
                    },
                };
        }

        switch (action.collection) {
            case 'contracts':
                return contractsReducer(state, action as CollectionAction<AppState, 'contracts'>);

            case 'positions':
                return positionsReducer(state, action as CollectionAction<AppState, 'positions'>);
        }

        return state;
    }
);
