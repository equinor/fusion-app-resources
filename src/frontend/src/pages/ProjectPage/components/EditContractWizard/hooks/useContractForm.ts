import { useCallback } from 'react';
import Contract from '../../../../../models/contract';
import useForm from '../../../../../hooks/useForm';

const createDefaultState = (): Contract => ({
    id: null,
    contractNumber: null,
    name: '',
    description: '',
    company: null,
    companyRepPositionId: null,
    contractResponsiblePositionId: null,
    endDate: null,
    externalCompanyRepPositionId: null,
    externalContractResponsiblePositionId: null,
    startDate: new Date(),
});

const useContractForm = (defaultState?: Contract | null) => {
    const validateForm = useCallback((formState: Contract) => {
        return Boolean(
            formState.contractNumber &&
                formState.startDate &&
                formState.endDate
        );
    }, []);

    return useForm(createDefaultState, validateForm, defaultState);
};

export default useContractForm;
