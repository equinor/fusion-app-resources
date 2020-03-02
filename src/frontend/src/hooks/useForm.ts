import { useState, useCallback, useMemo, useEffect } from "react";
const deepEqual = require('deep-equal');

const useForm = <T>(createDefaultState: () => T, validateForm: (formState: T) => boolean, defaultState?: T | null) => {
    const [formState, setState] = useState<T>(defaultState || createDefaultState());

    const [initialState, setInitialState] = useState<T>(formState);
    const isFormDirty = useMemo(() => {
        return !deepEqual(formState, initialState);
    }, [formState, initialState]);

    useEffect(() => {
        if(defaultState) {
            setState(defaultState);
            setInitialState(defaultState);
        }
    }, [defaultState]);

    const isFormValid = useMemo(() => {
        return validateForm(formState);
    }, [formState, validateForm]);

    const setFormField = useCallback(
        <TKey extends keyof T>(key: TKey, value: T[TKey]) => {
            setState(previousState => ({
                ...previousState,
                [key]: value,
            }));
        },
        []
    );

    const formFieldSetter = useCallback(
        <TKey extends keyof T>(key: TKey) => (value: T[TKey]) => {
            setState(previousState => ({
                ...previousState,
                [key]: value,
            }));
        },
        []
    );

    const resetForm = useCallback(() => {
        setState(createDefaultState());
    }, [createDefaultState]);

    return { formState, isFormValid, setFormField, formFieldSetter, resetForm, isFormDirty };
};

export default useForm;
