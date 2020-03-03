import { useCallback, useEffect } from 'react';
import useForm from '../../../../../hooks/useForm';
import CreatePositionRequest from '../../../../../models/createPositionRequest';
const createDefaultState = (): CreatePositionRequest => ({
    basePosition: null,
    name: '',
    appliesFrom: new Date(),
    appliesTo: null,
    assignedPerson: null,
    workload: 100,
});

const useCreatePositionForm = (defaultState?: CreatePositionRequest | null) => {
    const validateForm = useCallback((formState: CreatePositionRequest) => {
        return Boolean(
            formState.basePosition &&
                formState.name &&
                formState.appliesFrom &&
                formState.appliesTo &&
                formState.assignedPerson &&
                formState.workload > -1
        );
    }, []);

    const form = useForm(createDefaultState, validateForm, defaultState);

    const basePosition = form.formState.basePosition;
    useEffect(() => {
        if(basePosition && basePosition.name) {
            form.setFormField('name', basePosition.name);
        }
    }, [basePosition]);

    return form;
};

export default useCreatePositionForm;
