import ApiClient from './api/ApiClient';
import { createContext, useContext, Dispatch } from 'react';
import { AppState } from './reducers/appReducer';
import { CollectionAction } from './reducers/utils';
import { useBasePositionsContext } from './hooks/useBasePositionsContext';

export interface IAppContext {
    apiClient: ApiClient;
    appState: AppState;
    useBasePositions: useBasePositionsContext;
    dispatchAppAction: Dispatch<CollectionAction<AppState, keyof AppState>>;
}

const AppContext = createContext<IAppContext>({} as IAppContext);

export const useAppContext = () => useContext(AppContext);

export default AppContext;
