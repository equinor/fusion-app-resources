import { useState, useCallback, useMemo, useEffect } from "react";

const useForm = <T>(createDefaultState: () => T, validateForm: (formState: T) => boolean, defaultState?: T | null) => {
    const [formState, setState] = useState<T>(defaultState || createDefaultState());

    useEffect(() => {
        if(defaultState) {
            setState(defaultState);
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

    return { formState, isFormValid, setFormField, formFieldSetter, resetForm };
};

export default useForm;
