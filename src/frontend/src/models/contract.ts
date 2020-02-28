import Company from "./company";

type Contract = {
    id: string | null;
    contractNumber: string | null;
    name: string | null;
    description: string | null;
    startDate: Date | null;
    endDate: Date | null;
    company: Company | null;
    contractResponsiblePositionId: string | null;
    companyRepPositionId: string | null;
    externalContractResponsiblePositionId: string | null;
    externalCompanyRepPositionId: string | null;
};

export default Contract;