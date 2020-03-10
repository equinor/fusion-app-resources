import { createContext, useContext, Dispatch } from "react";
import Contract from './models/contract';
import { CollectionAction } from './reducers/utils';
import { ContractState } from './reducers/contractReducer';
import PersonnelRequest from './models/PersonnelRequest';
export interface IContractContext {
    contract: Contract | null;
    isFetchingContract: boolean;
    editRequests: PersonnelRequest[] | null;
    setEditRequests: (request: PersonnelRequest[] | null) => void;
    contractState: ContractState;
    dispatchContractAction: Dispatch<CollectionAction<ContractState, keyof ContractState>>;
};

const ContractContext = createContext<IContractContext>({} as IContractContext);

export const useContractContext = () => useContext(ContractContext);

export default ContractContext;
