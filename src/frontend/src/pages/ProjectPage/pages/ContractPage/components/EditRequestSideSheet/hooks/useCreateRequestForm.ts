import { useCallback } from 'react';
import CreatePersonnelRequest from '../../../../../../../models/CreatePersonnelRequest';
import { v1 as uuid } from 'uuid';
import useForm from '../../../../../../../hooks/useForm';

const createDefaultState = (): CreatePersonnelRequest => ({
    description: '',
    id: uuid(),
    person: null,
    position: null,
});

const useCreatePositionForm = (defaultState?: CreatePersonnelRequest | null) => {
    const validateForm = useCallback((formState: CreatePersonnelRequest) => {
        return Boolean(
            formState.description && formState.id && formState.person && formState.position
        );
    }, []);

    return useForm(createDefaultState, validateForm, defaultState);
};

export default useCreatePositionForm;
