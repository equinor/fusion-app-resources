
import {
    Stepper,
    Step,
    Button,
    TextInput,
    DatePicker,
    IconButton,
    useTooltipRef,
    Spinner,
    CloseIcon,
} from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './hooks/useContractForm';
import ContractNumberPicker from './components/ContractNumberPicker';
import classNames from 'classnames';
import styles from './styles.less';
import ContractPositionPicker from './components/ContractPositionPicker';
import CreateOrEditExternalPositionButton from './components/CreateOrEditExternalPositionButton';
import { formatDate, useTelemetryLogger, useNotificationCenter } from '@equinor/fusion';
import CompanyPicker from './components/CompanyPicker';
import useContractAllocationAutoFocus from './hooks/useContractAllocationAutoFocus';
import useActiveStepKey, { StepKey } from './hooks/useActiveStepKey';
import useContractPersister from './hooks/useContractPersister';
import { FC, useMemo, useCallback } from 'react';

export { default as ContractWizardSkeleton } from './components/ContractWizardSkeleton';

type EditContractWizardProps = {
    title: string;
    existingContract?: Contract;
    onCancel: () => void;
    goBackTo: string;
    onGoBack: () => void;
    onSubmit: (contract: Contract) => void;
};

const EditContractWizard: FC<EditContractWizardProps> = ({
    title,
    existingContract,
    onCancel,
    goBackTo,
    onGoBack,
    onSubmit,
}) => {
    const isEdit = useMemo(() => {
        return Boolean(existingContract && existingContract.contractNumber !== null);
    }, [existingContract]);

    const {
        formState,
        resetForm,
        formFieldSetter,
        setFormField,
        isFormValid,
        isFormDirty,
    } = useContractForm(existingContract);

    const { saveAsync, isSaving } = useContractPersister(formState);

    const telemetryLogger = useTelemetryLogger();
    const sendNotification = useNotificationCenter();

    const onSave = useCallback(async () => {
        try {
            const savedContract = await saveAsync();
            if (formState.id) {
                resetForm(savedContract);
            } else {
                setFormField('id', savedContract.id);
            }
        } catch (e) {
            telemetryLogger.trackException(e);
            sendNotification({
                level: 'medium',
                title: 'Unable to save contract. Please try again',
            });
        }
    }, [formState, resetForm, setFormField, saveAsync]);

    const {
        setActiveStepKey,
        activeStepKey,
        gotoContract,
        gotoContractDetails,
        gotoExteral,
    } = useActiveStepKey(isEdit, formState, onSave);

    const {
        contractNumberRef,
        nameInputRef,
        externalCompanyRepRef,
    } = useContractAllocationAutoFocus(activeStepKey, formState);

    const contractDetailsDescription = useMemo(() => {
        const dateOrNa = (date: Date | null) => (date ? formatDate(date) : 'N/A');
        return `${formState.company ? formState.company.name + ' - ' : ''} ${dateOrNa(
            formState.startDate
        )} - ${dateOrNa(formState.endDate)}`;
    }, [formState]);

    const backButtonTooltipRef = useTooltipRef('Go back to ' + goBackTo, 'right');

    const clearEquinorContractResponsible = useCallback(
        () => setFormField('contractResponsible', null),
        [setFormField]
    );
    const clearEquinorCompanyRep = useCallback(() => setFormField('companyRep', null), [
        setFormField,
    ]);
    const clearExternalContractResponsible = useCallback(
        () => setFormField('externalContractResponsible', null),
        [setFormField]
    );
    const clearExternalCompanyRep = useCallback(
        () => setFormField('externalCompanyRep', null),
        [setFormField]
    );

    const handleSubmit = useCallback(async () => {
        try {
            const contract = await saveAsync();
            onSubmit(contract);
        } catch (e) {
            telemetryLogger.trackException(e);
            sendNotification({
                level: 'medium',
                title: 'Unable to save contract. Please try again',
            });
        }
    }, [saveAsync, onSubmit]);

    const handleChange = useCallback((stepKey: string) => {
        setActiveStepKey(stepKey as StepKey);
    }, []);

    return (
        <div className={styles.container}>
            <header className={styles.header}>
                <IconButton data-cy="close-btn" onClick={onGoBack} ref={backButtonTooltipRef}>
                    <CloseIcon />
                </IconButton>
                <h2>{title}</h2>
                <Button outlined onClick={onCancel}>
                    Cancel
                </Button>
                <Button
                    outlined
                    disabled={!isFormValid || !isFormDirty || isSaving}
                    onClick={onSave}
                >
                    {isSaving ? (
                        <>
                            <Spinner inline size={16} /> Saving
                        </>
                    ) : (
                        'Save'
                    )}
                </Button>
            </header>
            <Stepper hideNavButtons activeStepKey={activeStepKey} onChange={handleChange}>
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
                                    selectedPosition={formState.contractResponsible}
                                    onSelect={formFieldSetter('contractResponsible')}
                                />
                                {formState.contractResponsible && (
                                    <Button frameless onClick={clearEquinorContractResponsible}>
                                        Clear
                                    </Button>
                                )}
                            </div>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="Equinor Company rep"
                                    selectedPosition={formState.companyRep}
                                    onSelect={formFieldSetter('companyRep')}
                                />
                                {formState.companyRep && (
                                    <Button frameless onClick={clearEquinorCompanyRep}>
                                        Clear
                                    </Button>
                                )}
                            </div>
                        </div>

                        <div className={styles.actions}>
                            {!isEdit && (
                                <Button outlined onClick={gotoContract}>
                                    Previous
                                </Button>
                            )}
                            {isSaving ? (
                                <Button disabled>
                                    <Spinner inline size={16} /> Saving
                                </Button>
                            ) : (
                                <Button onClick={gotoExteral} disabled={!isFormValid}>
                                    {formState.id ? 'Next' : 'Save and next'}
                                </Button>
                            )}
                        </div>
                    </div>
                </Step>
                <Step title="External" stepKey="external" disabled={formState.id === null}>
                    <div className={styles.stepContainer}>
                        <div className={styles.row}>
                            <div className={styles.field} ref={externalCompanyRepRef}>
                                <ContractPositionPicker
                                    label="External Company rep"
                                    contractId={formState.id || undefined}
                                    selectedPosition={formState.externalCompanyRep}
                                    onSelect={formFieldSetter('externalCompanyRep')}
                                />
                                {formState.externalCompanyRep && (
                                    <Button frameless onClick={clearExternalCompanyRep}>
                                        Clear
                                    </Button>
                                )}
                                <CreateOrEditExternalPositionButton
                                    repType="company-rep"
                                    contract={formState}
                                    existingPosition={formState.externalCompanyRep}
                                    onComplete={formFieldSetter('externalCompanyRep')}
                                />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="External Contract responsible"
                                    contractId={formState.id || undefined}
                                    selectedPosition={formState.externalContractResponsible}
                                    onSelect={formFieldSetter('externalContractResponsible')}
                                />
                                {formState.externalContractResponsible && (
                                    <Button frameless onClick={clearExternalContractResponsible}>
                                        Clear
                                    </Button>
                                )}
                                <CreateOrEditExternalPositionButton
                                    repType="contract-responsible"
                                    contract={formState}
                                    existingPosition={formState.externalContractResponsible}
                                    onComplete={formFieldSetter('externalContractResponsible')}
                                />
                            </div>
                        </div>
                        <div className={styles.actions}>
                            <Button outlined onClick={gotoContractDetails}>
                                Previous
                            </Button>
                            <Button
                                disabled={!isFormValid || !isFormDirty || isSaving}
                                onClick={handleSubmit}
                            >
                                {isSaving ? (
                                    <>
                                        <Spinner inline size={16} /> Submitting
                                    </>
                                ) : (
                                    'Submit'
                                )}
                            </Button>
                        </div>
                    </div>
                </Step>
            </Stepper>
        </div>
    );
};

export default EditContractWizard;
