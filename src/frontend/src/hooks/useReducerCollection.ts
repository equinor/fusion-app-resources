import { CollectionAction, ReadonlyCollection, ExtractCollectionType } from '../reducers/utils';
import { useCallback, useEffect } from 'react';
import { useDebouncedAbortable } from '@equinor/fusion';

const useReducerCollection = <TState, T extends keyof TState>(
    state: TState,
    dispatch: React.Dispatch<CollectionAction<TState, T>>,
    collection: T,
    fetcher?: () => Promise<ReadonlyCollection<ExtractCollectionType<TState[T]>>['data']>
): TState[T] => {
    const fetch = useCallback(async () => {
        if (!fetcher) {
            return;
        }

        try {
            dispatch({
                verb: 'fetch',
                collection,
            });

            const data = await fetcher();

            dispatch({
                verb: 'merge',
                collection,
                payload: data,
            });
        } catch (error) {
            dispatch({
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

    return state[collection];
};

export default useReducerCollection;
