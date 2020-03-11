import ApiClient from './api/ApiClient';
import { createContext, useContext, Dispatch } from 'react';
import { AppState } from './reducers/appReducer';
import { CollectionAction } from './reducers/utils';

export interface IAppContext {
    apiClient: ApiClient;
    appState: AppState;
    dispatchAppAction: Dispatch<CollectionAction<AppState, keyof AppState>>;
}

const AppContext = createContext<IAppContext>({} as IAppContext);

export const useAppContext = () => useContext(AppContext);

export default AppContext;
