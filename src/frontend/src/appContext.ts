import ApiClient from './api/ApiClient';
import { createContext, useContext } from 'react';

export interface IAppContext {
    apiClient: ApiClient
};

const AppContext = createContext<IAppContext>({} as IAppContext);

export const useAppContext = () => useContext(AppContext);

export default AppContext;