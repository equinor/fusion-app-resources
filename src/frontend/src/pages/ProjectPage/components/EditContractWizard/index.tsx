import * as React from 'react';
import { Stepper, Step } from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './useContractForm';

type EditContractWizardProps = {
    existingContract?: Contract;
};

const EditContractWizard: React.FC<EditContractWizardProps> = ({ existingContract }) => {
    const { formState } = useContractForm(existingContract);

    const isEdit = React.useMemo(() => {
        return existingContract && existingContract.contractNumber !== null;
    }, [existingContract]);

    return (
        <div>
            <Stepper activeStepKey={isEdit ? 'contract-details' : 'select-contract'}>
                <Step
                    title="Select contract"
                    stepKey="select-contract"
                    disabled={formState.contractNumber !== null}
                >
                    <div>Dropdown</div>
                </Step>
                <Step
                    title="Contract details"
                    stepKey="contract-details"
                    disabled={formState.contractNumber === null}
                >
                    <div>Dropdown</div>
                </Step>
                <Step
                    title="External"
                    stepKey="external"
                    disabled={formState.contractNumber === null}
                >
                    <div>Dropdown</div>
                </Step>
            </Stepper>
        </div>
    );
};

export default EditContractWizard;
