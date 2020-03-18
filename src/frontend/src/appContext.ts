import ApiClient from './api/ApiClient';
import { createContext, useContext, Dispatch } from 'react';
import { AppState } from './reducers/appReducer';
import { CollectionAction } from './reducers/utils';
import ServiceNowApiClient from './api/ServiceNowApiClient';

export interface IAppContext {
    apiClient: ApiClient;
    serviceNowApiClient: ServiceNowApiClient;
    appState: AppState;
    dispatchAppAction: Dispatch<CollectionAction<AppState, keyof AppState>>;
}

const AppContext = createContext<IAppContext>({} as IAppContext);

export const useAppContext = () => useContext(AppContext);

export default AppContext;
