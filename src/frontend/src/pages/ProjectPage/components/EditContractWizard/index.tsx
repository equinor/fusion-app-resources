import * as React from 'react';
import {
    Stepper,
    Step,
    Button,
    TextInput,
    DatePicker,
    ArrowBackIcon,
    IconButton,
} from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './hooks/useContractForm';
import ContractNumberSelector from './components/ContractNumberSelector';
import classNames from 'classnames';
import * as styles from './styles.less';
import ContractPositionPicker from './components/ContractPositionPicker';
import NewPositionSidesheet from './components/NewPositionSidesheet';
import { useAppContext } from '../../../../appContext';
import { useCurrentContext } from '@equinor/fusion';
import CompanyPicker from './components/CompanyPicker';

type EditContractWizardProps = {
    title: string;
    existingContract?: Contract;
};

const EditContractWizard: React.FC<EditContractWizardProps> = ({ title, existingContract }) => {
    const isEdit = React.useMemo(() => {
        return existingContract && existingContract.contractNumber !== null;
    }, [existingContract]);
    const { formState, formFieldSetter, setFormField, isFormValid, isFormDirty } = useContractForm(
        existingContract
    );

    const conContinue = React.useMemo(() => {
        return formState.contractNumber !== null;
    }, [formState]);

    const [activeStepKey, setActiveStepKey] = React.useState(
        isEdit ? 'contract-details' : 'select-contract'
    );

    const gotoContract = React.useCallback(() => setActiveStepKey('select-contract'), []);
    const gotoContractDetails = React.useCallback(() => setActiveStepKey('contract-details'), []);

    const { apiClient } = useAppContext();
    const project = useCurrentContext() as any;
    const gotoExteral = React.useCallback(async () => {
        if (!formState.id) {
            const response = await apiClient.createContractAsync(project.externalId, formState);
            const createdContract = response.data;
            setFormField('id', createdContract.id);
        }

        setActiveStepKey('external');
    }, [formState]);

    React.useEffect(() => {
        if (formState.contractNumber) {
            gotoContractDetails();
        }
    }, [formState.contractNumber]);

    return (
        <div>
            <header className={styles.header}>
                <IconButton>
                    <ArrowBackIcon />
                </IconButton>
                <h2>{title}</h2>
                <Button outlined>Cancel</Button>
                <Button outlined disabled={!isFormValid || !isFormDirty}>
                    Save
                </Button>
            </header>
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
                            <div className={classNames(styles.field, styles.big)}>
                                <CompanyPicker
                                    selectedCompanyId={formState.company?.id || null}
                                    onSelect={formFieldSetter('company')}
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
                                    label="Equinor Contract responsible"
                                    selectedPositionId={formState.contractResponsiblePositionId}
                                    onSelect={formFieldSetter('contractResponsiblePositionId')}
                                />
                            </div>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="Equinor Company rep"
                                    selectedPositionId={formState.companyRepPositionId}
                                    onSelect={formFieldSetter('companyRepPositionId')}
                                />
                            </div>
                        </div>

                        <div className={styles.actions}>
                            <Button outlined onClick={gotoContract}>
                                Previous
                            </Button>
                            <Button onClick={gotoExteral} disabled={!isFormValid}>
                                Next
                            </Button>
                        </div>
                    </div>
                </Step>
                <Step title="External" stepKey="external" disabled={!conContinue}>
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="External Company rep"
                                    selectedPositionId={formState.externalCompanyRepPositionId}
                                    onSelect={formFieldSetter('externalCompanyRepPositionId')}
                                />
                                <NewPositionSidesheet
                                    repType="company-rep"
                                    contract={formState}
                                    setCompanyRepPosition={formFieldSetter(
                                        'externalCompanyRepPositionId'
                                    )}
                                    setContractResponsiblePosition={formFieldSetter(
                                        'externalContractResponsiblePositionId'
                                    )}
                                />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="External Contract responsible"
                                    selectedPositionId={
                                        formState.externalContractResponsiblePositionId
                                    }
                                    onSelect={formFieldSetter(
                                        'externalContractResponsiblePositionId'
                                    )}
                                />
                                <NewPositionSidesheet
                                    repType="contract-responsible"
                                    contract={formState}
                                    setCompanyRepPosition={formFieldSetter(
                                        'externalCompanyRepPositionId'
                                    )}
                                    setContractResponsiblePosition={formFieldSetter(
                                        'externalContractResponsiblePositionId'
                                    )}
                                />
                            </div>
                        </div>
                        <div className={styles.actions}>
                            <Button outlined onClick={gotoContractDetails}>
                                Previous
                            </Button>
                            <Button onClick={gotoExteral} disabled={!isFormValid}>
                                Submit
                            </Button>
                        </div>
                    </div>
                </Step>
            </Stepper>
        </div>
    );
};

export default EditContractWizard;
