import * as React from 'react';
import {
    Stepper,
    Step,
    Button,
    TextInput,
    DatePicker,
    ArrowBackIcon,
    IconButton,
    useTooltipRef,
} from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './hooks/useContractForm';
import ContractNumberPicker from './components/ContractNumberPicker';
import classNames from 'classnames';
import * as styles from './styles.less';
import ContractPositionPicker from './components/ContractPositionPicker';
import NewPositionSidesheet from './components/NewPositionSidesheet';
import { useAppContext } from '../../../../appContext';
import { useCurrentContext, formatDate } from '@equinor/fusion';
import CompanyPicker from './components/CompanyPicker';

type EditContractWizardProps = {
    title: string;
    existingContract?: Contract;
    onCancel: () => void;
    goBackTo: string;
    onGoBack: () => void;
};

type StepKey = 'select-contract' | 'contract-details' | 'external';

const EditContractWizard: React.FC<EditContractWizardProps> = ({
    title,
    existingContract,
    onCancel,
    goBackTo,
    onGoBack,
}) => {
    const isEdit = React.useMemo(() => {
        return existingContract && existingContract.contractNumber !== null;
    }, [existingContract]);

    const {
        formState,
        resetForm,
        formFieldSetter,
        setFormField,
        isFormValid,
        isFormDirty,
    } = useContractForm(existingContract);

    const [activeStepKey, setActiveStepKey] = React.useState<StepKey>(
        isEdit ? 'contract-details' : 'select-contract'
    );

    const { apiClient } = useAppContext();
    const project = useCurrentContext() as any;
    const saveAsync = React.useCallback(async () => {
        if (formState.id) {
            const updatedContract = await apiClient.updateContractAsync(
                project.externalId,
                formState.id,
                formState
            );
            resetForm(updatedContract);
        } else {
            const createdContract = await apiClient.createContractAsync(
                project.externalId,
                formState
            );
            setFormField('id', createdContract.id);
        }
    }, [formState]);

    const gotoContract = React.useCallback(() => setActiveStepKey('select-contract'), []);
    const gotoContractDetails = React.useCallback(() => setActiveStepKey('contract-details'), []);

    const gotoExteral = React.useCallback(async () => {
        if (!formState.id) {
            await saveAsync();
        }

        setActiveStepKey('external');
    }, [formState]);

    const contractNumberRef = React.useRef<HTMLDivElement>(null);
    const nameInputRef = React.useRef<HTMLInputElement>(null);
    const externalCompanyRepRef = React.useRef<HTMLDivElement>(null);
    React.useEffect(() => {
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

    React.useEffect(() => {
        if (formState.contractNumber) {
            gotoContractDetails();
        }
    }, [formState.contractNumber]);

    const contractDetailsDescription = React.useMemo(() => {
        const dateOrNa = (date: Date | null) => (date ? formatDate(date) : 'N/A');
        return `${formState.company ? formState.company.name + ' - ' : ''} ${dateOrNa(
            formState.startDate
        )} - ${dateOrNa(formState.endDate)}`;
    }, [formState]);

    const backButtonTooltipRef = useTooltipRef('Go back to ' + goBackTo, 'right');

    return (
        <div>
            <header className={styles.header}>
                <IconButton onClick={onGoBack} ref={backButtonTooltipRef}>
                    <ArrowBackIcon />
                </IconButton>
                <h2>{title}</h2>
                <Button outlined onClick={onCancel}>
                    Cancel
                </Button>
                <Button outlined disabled={!isFormValid || !isFormDirty} onClick={saveAsync}>
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
                        <h2>Select a contract to continue</h2>
                        <div className={styles.row} ref={contractNumberRef}>
                            <ContractNumberPicker
                                selectedContractNumber={formState.contractNumber}
                                onSelect={formFieldSetter('contractNumber')}
                            />
                        </div>
                    </div>
                </Step>
                <Step
                    title="Contract details"
                    stepKey="contract-details"
                    disabled={formState.contractNumber === null}
                    description={contractDetailsDescription}
                >
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={classNames(styles.field, styles.big)}>
                                <TextInput
                                    ref={nameInputRef}
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
                                {formState.id ? 'Next' : 'Save and next'}
                            </Button>
                        </div>
                    </div>
                </Step>
                <Step title="External" stepKey="external" disabled={formState.id === null}>
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={styles.field} ref={externalCompanyRepRef}>
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
                            <Button disabled={!isFormValid} onClick={saveAsync}>
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
