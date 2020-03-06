import ApiClient from './api/ApiClient';
import { createContext, useContext, Dispatch } from 'react';
import { AppState, AppAction } from './appReducer';

export interface IAppContext {
    apiClient: ApiClient;
    appState: AppState;
    dispatchAppAction: Dispatch<AppAction<keyof AppState>>;
}

const AppContext = createContext<IAppContext>({} as IAppContext);

export const useAppContext = () => useContext(AppContext);

export default AppContext;
