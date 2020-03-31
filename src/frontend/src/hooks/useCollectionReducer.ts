import { useReducer, useEffect, useMemo } from 'react';
import JSON from '@equinor/fusion/lib/utils/JSON';
import { RootReducer } from "../reducers/utils";

const useCollectionReducer = <TState>(key: string, reducer: RootReducer<TState>, initialState: TState) => {
    const SESSION_STATE_KEY = 'FUSION_RESOURCES_STATE:' + key;
    const sessionState = sessionStorage.getItem(SESSION_STATE_KEY);

    const state = useMemo(() => sessionState ? JSON.parse(sessionState) as TState : initialState, [sessionState, initialState]);

    const result = useReducer(reducer, { ...initialState, ...state });

    useEffect(() => {
        sessionStorage.setItem(SESSION_STATE_KEY, JSON.stringify(result[0]));
    }, [SESSION_STATE_KEY, result[0]]);

    return result;
};

export default useCollectionReducer;