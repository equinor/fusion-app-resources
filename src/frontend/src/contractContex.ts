import { createContext, useContext } from "react";
import { Contract } from '@equinor/fusion';

export interface IContractContext {
    contract: Contract;
};

const ContractContext = createContext<IContractContext | null>(null);

export const useContractContext = () => useContext(ContractContext);

export default ContractContext;