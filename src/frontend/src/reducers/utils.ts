export type ReadonlyCollection<T> = {
    data: T[];
    isFetching: Readonly<boolean>;
    error: Readonly<Error | null>;
};

export type ActionVerb = 'fetch' | 'error' | 'merge';

export type ExtractCollectionType<C> = C extends ReadonlyCollection<infer T> ? T : never;

export type CollectionAction<TState, T extends keyof TState> = {
    collection: T;
    verb: ActionVerb;
    payload?: ReadonlyCollection<ExtractCollectionType<TState[T]>>['data'];
    error?: Error;
};

export const merge = <T>(existing: T[], compare: (x: T, y: T) => boolean, payload?: T[]): T[] => {
    const updatedItems = payload?.filter(x => existing.some(y => compare(x, y)));
    const newItems = payload?.filter(x => existing.every(y => !compare(x, y))) || [];

    return [...existing.map(x => updatedItems?.find(y => compare(x, y)) || x), ...newItems];
};

export type Reducer<TState> = <T extends keyof TState>(
    state: TState,
    action: CollectionAction<TState, T>
) => TState;

export const createCollectionReducer = <TState>(reducer: Reducer<TState>): Reducer<TState> => {
    return <T extends keyof TState>(state: TState, action: CollectionAction<TState, T>) => {
        switch (action.verb) {
            case 'fetch':
                const fetchingState = {
                    ...state,
                    [action.collection]: {
                        ...state[action.collection],
                        error: null,
                        isFetching: true,
                    },
                };
                return reducer(fetchingState, action);

            case 'error':
                const errorState = {
                    ...state,
                    [action.collection]: {
                        ...state[action.collection],
                        isFetching: true,
                        error: action.error,
                    },
                };

                return reducer(errorState, action);
        }
        return reducer(state, action);
    };
};
