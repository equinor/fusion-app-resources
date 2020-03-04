import { useCallback, useEffect } from 'react';
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

    const form = useForm(createDefaultState, validateForm, defaultState);

    useEffect(() => {
        form.setFormField('companyRepPositionId', form.formState.companyRep?.id || null);
    }, [form.formState.companyRep]);

    useEffect(() => {
        form.setFormField(
            'contractResponsiblePositionId',
            form.formState.contractResponsible?.id || null
        );
    }, [form.formState.contractResponsible]);

    useEffect(() => {
        form.setFormField(
            'externalCompanyRepPositionId',
            form.formState.externalCompanyRep?.id || null
        );
    }, [form.formState.externalCompanyRep]);

    useEffect(() => {
        form.setFormField(
            'externalContractResponsiblePositionId',
            form.formState.externalContractResponsible?.id || null
        );
    }, [form.formState.externalContractResponsible]);

    return form;
};

export default useContractForm;
