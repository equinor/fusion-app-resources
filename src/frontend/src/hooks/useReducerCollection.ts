import { CollectionAction, ReadonlyCollection, ExtractCollectionType, ActionVerb } from '../reducers/utils';
import { Dispatch, useCallback, useEffect } from 'react';
import { useTelemetryLogger } from '@equinor/fusion';

const useReducerCollection = <TState, T extends keyof TState>(
    state: TState,
    dispatch: Dispatch<CollectionAction<TState, T>>,
    collection: T,
    fetcher?: () => Promise<ReadonlyCollection<ExtractCollectionType<TState[T]>>['data']>,
    verb: ActionVerb = 'merge'
): TState[T] => {
    const telemetryLogger = useTelemetryLogger();

    const fetch = useCallback(async (abortSignal: AbortSignal) => {
        if (!fetcher || abortSignal.aborted) {
            return;
        }

        try {
            dispatch({
                verb: 'fetch',
                collection,
            });

            const data = await fetcher();

            if(abortSignal.aborted) {
                return;
            }

            dispatch({
                verb,
                collection,
                payload: data,
            });
        } catch (error) {
            dispatch({
                verb: 'error',
                collection,
                error,
            });

            telemetryLogger.trackException(error);
        }
    }, [fetcher]);

    useEffect(() => {
        const abortController = new AbortController();

        fetch(abortController.signal);

        return () => abortController.abort();
    }, [fetch]);

    return state[collection];
};

export default useReducerCollection;
