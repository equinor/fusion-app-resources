export type ReadonlyCollection<T> = {
    data: T[];
    isFetching: Readonly<boolean>;
    error: Readonly<Error | null>;
};

export const createEmptyCollection = <T>(): ReadonlyCollection<T> => ({
    data: [],
    isFetching: false,
    error: null,
});

export type ActionVerb = 'fetch' | 'error' | 'merge' | 'delete';

export type ExtractCollectionType<C> = C extends ReadonlyCollection<infer T> ? T : never;

export type CollectionAction<
    TState,
    TCollection extends keyof TState,
    T = ExtractCollectionType<TState[TCollection]>
    > = {
        collection: TCollection;
        verb: ActionVerb;
        payload?: ReadonlyCollection<T>['data'];
        error?: Error;
    };

export const merge = <T>(existing: T[], compare: (x: T, y: T) => boolean, payload?: T[]): T[] => {
    const updatedItems = payload?.filter(x => existing.some(y => compare(x, y)));
    const newItems = payload?.filter(x => existing.every(y => !compare(x, y))) || [];

    return [...existing.map(x => updatedItems?.find(y => compare(x, y)) || x), ...newItems];
};

export const remove = <T>(existing: T[], compare: (x: T, y: T) => boolean, payload?: T[]): T[] => {
    if (!payload?.length) return existing

    return existing.filter(e => !payload.some(x => compare(e, x)));
};

export type RootReducer<TState> = <T extends keyof TState>(
    state: TState,
    action: CollectionAction<TState, T>
) => TState;

export type Reducer<
    TState,
    TCollection extends keyof TState,
    T = ExtractCollectionType<TState[TCollection]>
    > = (state: TState, action: CollectionAction<TState, TCollection, T>) => TState;

export const createCollectionReducer = <
    TState,
    TCollection extends keyof TState,
    T = ExtractCollectionType<TState[TCollection]>
>(
    compare: (x: T, y: T) => boolean,
    reducer: Reducer<TState, TCollection, T> = state => state
) => {
    return (state: TState, action: CollectionAction<TState, TCollection, T>) => {

        const existing = ((state[action.collection] as unknown) as ReadonlyCollection<T>).data;

        switch (action.verb) {
            case 'merge':
                return reducer(
                    {
                        ...state,
                        [action.collection]: {
                            isFetching: false,
                            error: null,
                            data: merge(existing, compare, action.payload),
                        },
                    },
                    action
                );

            case 'delete':
                return reducer(
                    {
                        ...state,
                        [action.collection]: {
                            isFetching: false,
                            error: null,
                            data: remove(existing, compare, action.payload),
                        },
                    },
                    action
                );
        }

        return reducer(state, action);
    };
};

export const createCollectionRootReducer = <TState>(
    reducer: RootReducer<TState>
): RootReducer<TState> => {
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
