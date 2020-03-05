import { createContext, useContext } from 'react';
import Contract from './models/contract';
import PersonnelRequest from './models/PersonnelRequest';

export interface IContractContext {
    contract: Contract | null;
    isFetchingContract: boolean;
    editRequests: PersonnelRequest[] | null;
    setEditRequests: (request: PersonnelRequest[] | null) => void;
}

const ContractContext = createContext<IContractContext>({} as IContractContext);

export const useContractContext = () => useContext(ContractContext);

export default ContractContext;
