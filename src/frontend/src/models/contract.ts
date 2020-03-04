import Company from './company';
import { Position } from '@equinor/fusion';

export type ContractReference = {
    id: string | null;
    contractNumber: string | null;
    name: string | null;
    company: Company | null;
};

type Contract = ContractReference & {
    description: string | null;
    startDate: Date | null;
    endDate: Date | null;
    contractResponsible: Position | null;
    contractResponsiblePositionId: string | null;
    companyRep: Position | null;
    companyRepPositionId: string | null;
    externalContractResponsible: Position | null;
    externalContractResponsiblePositionId: string | null;
    externalCompanyRep: Position | null;
    externalCompanyRepPositionId: string | null;
};

export default Contract;
