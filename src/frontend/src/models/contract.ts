import Company from './company';

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
    contractResponsiblePositionId: string | null;
    companyRepPositionId: string | null;
    externalContractResponsiblePositionId: string | null;
    externalCompanyRepPositionId: string | null;
};

export default Contract;
