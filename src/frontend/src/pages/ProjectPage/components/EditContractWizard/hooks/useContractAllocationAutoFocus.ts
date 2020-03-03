import { useEffect, useRef } from 'react';
import Contract from '../../../../../models/contract';
import { StepKey } from './useActiveStepKey';

const useContractAllocationAutoFocus = (activeStepKey: StepKey, formState: Contract) => {
    const contractNumberRef = useRef<HTMLDivElement>(null);
    const nameInputRef = useRef<HTMLInputElement>(null);
    const externalCompanyRepRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const focusTimer = setTimeout(() => {
            if (
                activeStepKey === 'select-contract' &&
                contractNumberRef.current &&
                !formState.contractNumber
            ) {
                contractNumberRef.current.querySelector('input')?.click();
            } else if (activeStepKey === 'contract-details' && nameInputRef.current) {
                nameInputRef.current?.focus();
            } else if (activeStepKey === 'external' && externalCompanyRepRef.current) {
                externalCompanyRepRef.current.querySelector('input')?.click();
            }
        }, 0);

        return () => clearTimeout(focusTimer);
    }, [
        activeStepKey,
        contractNumberRef.current,
        nameInputRef.current,
        externalCompanyRepRef.current,
        formState.contractNumber,
    ]);

    return {
        contractNumberRef,
        nameInputRef,
        externalCompanyRepRef,
    };
};

export default useContractAllocationAutoFocus;
