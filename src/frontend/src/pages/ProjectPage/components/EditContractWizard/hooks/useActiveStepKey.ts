import { useState, useCallback, useEffect } from 'react';
import Contract from '../../../../../models/contract';

export type StepKey = 'select-contract' | 'contract-details' | 'external';

const useActiveStepKey = (isEdit: boolean, formState: Contract, saveAsync: () => Promise<void>) => {
    const [activeStepKey, setActiveStepKey] = useState<StepKey>(
        isEdit ? 'contract-details' : 'select-contract'
    );

    const gotoContract = useCallback(() => setActiveStepKey('select-contract'), []);
    const gotoContractDetails = useCallback(() => setActiveStepKey('contract-details'), []);

    const gotoExteral = useCallback(async () => {
        if (!formState.id) {
            await saveAsync();
        }

        setActiveStepKey('external');
    }, [formState]);

    useEffect(() => {
        if (formState.contractNumber) {
            gotoContractDetails();
        }
    }, [formState.contractNumber]);

    return {
        activeStepKey,
        setActiveStepKey,
        gotoContract,
        gotoContractDetails,
        gotoExteral,
    };
};

export default useActiveStepKey;
