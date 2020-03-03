import { createContext, useContext } from "react";
import Contract from './models/contract';

export interface IContractContext {
    contract: Contract | null;
    isFetchingContract: boolean;
};

const ContractContext = createContext<IContractContext>({} as IContractContext);

export const useContractContext = () => useContext(ContractContext);

export default ContractContext;