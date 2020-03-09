import Contract from '../models/contract';
import { Position } from '@equinor/fusion';
import {
    ReadonlyCollection,
    CollectionAction,
    createCollectionRootReducer,
    createCollectionReducer,
} from './utils';

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

const contractsReducer = createCollectionReducer<AppState, 'contracts'>(
    (x, y) => x.id === y.id,
    (state, action) => {
        switch (action.verb) {
            case 'merge':
                return appReducer(state, {
                    verb: 'merge',
                    collection: 'positions',
                    payload: extractPositionsFromContracts(state.contracts.data),
                });
        }

        return state;
    }
);

const positionsReducer = createCollectionReducer<AppState, 'positions'>((x, y) => x.id === y.id);

export const appReducer = createCollectionRootReducer(
    <T extends keyof AppState = keyof AppState>(
        state: AppState,
        action: CollectionAction<AppState, T>
    ): AppState => {
        switch (action.collection) {
            case 'contracts':
                return contractsReducer(state, action as CollectionAction<AppState, 'contracts'>);

            case 'positions':
                return positionsReducer(state, action as CollectionAction<AppState, 'positions'>);
        }

        return state;
    }
);
