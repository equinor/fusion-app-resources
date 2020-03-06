import Contract from './models/contract';
import { Position, useDebouncedAbortable } from '@equinor/fusion';
import { useAppContext } from './appContext';
import { useEffect, useCallback } from 'react';

type ReadonlyCollection<T> = {
    data: T[];
    isFetching: Readonly<boolean>;
    error: Readonly<Error | null>;
};

export type AppState = {
    contracts: ReadonlyCollection<Contract>;
    positions: ReadonlyCollection<Position>;
};

export type ActionVerb = 'fetch' | 'error' | 'set' | 'merge';

export type AppAction<T extends keyof AppState> = {
    collection: T;
    verb: ActionVerb;
    payload?: AppState[T]['data'];
    error?: Error;
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

const merge = <T>(existing: T[], compare: (x: T, y: T) => boolean, payload?: T[]): T[] => {
    const updatedItems = payload?.filter(x => existing.some(y => compare(x, y)));
    const newItems = payload?.filter(x => existing.every(y => !compare(x, y))) || [];

    return [...existing.map(x => updatedItems?.find(y => compare(x, y)) || x), ...newItems];
};

const contractsReducer = (state: AppState, action: AppAction<'contracts'>): AppState => {
    switch (action.verb) {
        case 'set':
            const stateWithContracts = {
                ...state,
                contracts: {
                    isFetching: false,
                    error: null,
                    data: action.payload || state.contracts.data,
                },
            };

            return appReducer(stateWithContracts, {
                verb: 'merge',
                collection: 'positions',
                payload: extractPositionsFromContracts(stateWithContracts.contracts.data),
            });

        case 'merge':
            return {
                ...state,
                contracts: {
                    isFetching: false,
                    error: null,
                    data: merge(state.contracts.data, (x, y) => x.id === y.id, action.payload),
                },
            };
    }

    return state;
};

const positionsReducer = (state: AppState, action: AppAction<'positions'>): AppState => {
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

export const appReducer = <T extends keyof AppState = keyof AppState>(
    state: AppState,
    action: AppAction<T>
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
            return contractsReducer(state, action as AppAction<'contracts'>);

        case 'positions':
            return positionsReducer(state, action as AppAction<'positions'>);
    }

    return state;
};

export const useReducerCollection = <T extends keyof AppState>(
    collection: T,
    fetcher?: () => Promise<AppState[T]['data']>
): AppState[T] => {
    const { appState, dispatchAppAction } = useAppContext();

    const fetch = useCallback(async () => {
        if (!fetcher) {
            return;
        }

        try {
            dispatchAppAction({
                verb: 'fetch',
                collection,
            });

            const data = await fetcher();

            dispatchAppAction({
                verb: 'merge',
                collection,
                payload: data,
            });
        } catch (error) {
            dispatchAppAction({
                verb: 'error',
                collection,
                error,
            });
        }
    }, [fetcher]);

    useDebouncedAbortable(fetch, void 0, 0);

    useEffect(() => {
        fetch();
    }, [fetch]);

    return appState[collection];
};
