import { useCallback } from 'react';
import Contract from '../../../../../models/contract';
import useForm from '../../../../../hooks/useForm';

const createDefaultState = (): Contract => ({
    id: null,
    contractNumber: null,
    name: '',
    description: '',
    company: null,
    companyRep: null,
    companyRepPositionId: null,
    contractResponsible: null,
    contractResponsiblePositionId: null,
    endDate: null,
    externalCompanyRep: null,
    externalCompanyRepPositionId: null,
    externalContractResponsible: null,
    externalContractResponsiblePositionId: null,
    startDate: new Date(),
});

const useContractForm = (defaultState?: Contract | null) => {
    const validateForm = useCallback((formState: Contract) => {
        return Boolean(formState.contractNumber && formState.company);
    }, []);

    return useForm(createDefaultState, validateForm, defaultState);
};

export default useContractForm;
