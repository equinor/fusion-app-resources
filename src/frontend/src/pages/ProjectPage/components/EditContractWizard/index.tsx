import * as React from 'react';
import {
    Stepper,
    Step,
    Button,
    TextInput,
    DatePicker,
} from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './hooks/useContractForm';
import ContractNumberSelector from './components/ContractNumberSelector';
import classNames from 'classnames';
import * as styles from './styles.less';
import ContractPositionPicker from './components/ContractPositionPicker';

type EditContractWizardProps = {
    existingContract?: Contract;
};

const EditContractWizard: React.FC<EditContractWizardProps> = ({ existingContract }) => {
    const isEdit = React.useMemo(() => {
        return existingContract && existingContract.contractNumber !== null;
    }, [existingContract]);
    const { formState, formFieldSetter } = useContractForm(existingContract);

    const conContinue = React.useMemo(() => {
        return formState.contractNumber !== null;
    }, [formState]);

    const [activeStepKey, setActiveStepKey] = React.useState(
        isEdit ? 'contract-details' : 'select-contract'
    );

    const gotoContract = React.useCallback(() => setActiveStepKey('select-contract'), []);
    const gotoContractDetails = React.useCallback(() => setActiveStepKey('contract-details'), []);
    const gotoExteral = React.useCallback(() => setActiveStepKey('external'), []);

    React.useEffect(() => {
        if (formState.contractNumber) {
            gotoContractDetails();
        }
    }, [formState.contractNumber]);

    return (
        <div>
            <Stepper activeStepKey={activeStepKey}>
                <Step
                    title="Select contract"
                    stepKey="select-contract"
                    disabled={isEdit}
                    description={formState.contractNumber || ''}
                >
                    <div className={styles.stepContainer}>
                        <ContractNumberSelector
                            selectedContractNumber={formState.contractNumber}
                            onSelect={formFieldSetter('contractNumber')}
                        />

                        <div className={styles.actions}>
                            <Button disabled={!conContinue} onClick={gotoContractDetails}>
                                Next
                            </Button>
                        </div>
                    </div>
                </Step>
                <Step title="Contract details" stepKey="contract-details" disabled={!conContinue}>
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={classNames(styles.field, styles.big)}>
                                <TextInput
                                    label="Contract name"
                                    value={formState.name || ''}
                                    onChange={formFieldSetter('name')}
                                />
                            </div>
                        </div>

                        <div className={styles.row}>
                            <div className={styles.field}>
                                <DatePicker
                                    label="From Date"
                                    selectedDate={formState.startDate}
                                    onChange={formFieldSetter('startDate')}
                                />
                            </div>
                            <div className={styles.field}>
                                <DatePicker
                                    label="To Date"
                                    selectedDate={formState.endDate}
                                    onChange={formFieldSetter('endDate')}
                                />
                            </div>
                        </div>

                        <div className={styles.row}>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="Equinor contract responsible"
                                    selectedPositionId={formState.contractResponsiblePositionId}
                                    onSelect={formFieldSetter('contractResponsiblePositionId')}
                                />
                            </div>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="Equinor company rep"
                                    selectedPositionId={formState.companyRepPositionId}
                                    onSelect={formFieldSetter('companyRepPositionId')}
                                />
                            </div>
                        </div>

                        <div className={styles.actions}>
                            <Button outlined onClick={gotoContract}>
                                Previous
                            </Button>
                            <Button onClick={gotoExteral}>Next</Button>
                        </div>
                    </div>
                </Step>
                <Step title="External" stepKey="external" disabled={!conContinue}>
                    <div className={styles.stepContainer}>
                        <div className={styles.actions}>
                            <Button outlined onClick={gotoContractDetails}>
                                Previous
                            </Button>
                        </div>
                    </div>
                </Step>
            </Stepper>
        </div>
    );
};

export default EditContractWizard;
