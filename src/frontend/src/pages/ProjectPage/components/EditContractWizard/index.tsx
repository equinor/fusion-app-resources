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
    Spinner,
} from '@equinor/fusion-components';
import Contract from '../../../../models/contract';
import useContractForm from './hooks/useContractForm';
import ContractNumberPicker from './components/ContractNumberPicker';
import classNames from 'classnames';
import * as styles from './styles.less';
import ContractPositionPicker from './components/ContractPositionPicker';
import NewPositionSidesheet from './components/NewPositionSidesheet';
import { formatDate } from '@equinor/fusion';
import CompanyPicker from './components/CompanyPicker';
import useContractAllocationAutoFocus from './hooks/useContractAllocationAutoFocus';
import useActiveStepKey from './hooks/useActiveStepKey';
import useContractPersister from './hooks/useContractPersister';

export { default as ContractWizardSkeleton } from './components/ContractWizardSkeleton';

type EditContractWizardProps = {
    title: string;
    existingContract?: Contract;
    onCancel: () => void;
    goBackTo: string;
    onGoBack: () => void;
};

const EditContractWizard: React.FC<EditContractWizardProps> = ({
    title,
    existingContract,
    onCancel,
    goBackTo,
    onGoBack,
}) => {
    const isEdit = React.useMemo(() => {
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

    const onSave = React.useCallback(
        (contract: Contract) => {
            if (formState.id) {
                resetForm(contract);
            } else {
                setFormField('id', contract.id);
            }
        },
        [formState, resetForm, setFormField]
    );
    const { saveAsync, isSaving } = useContractPersister(formState, onSave);

    const { activeStepKey, gotoContract, gotoContractDetails, gotoExteral } = useActiveStepKey(
        isEdit,
        formState,
        saveAsync
    );

    const {
        contractNumberRef,
        nameInputRef,
        externalCompanyRepRef,
    } = useContractAllocationAutoFocus(activeStepKey, formState);

    const contractDetailsDescription = React.useMemo(() => {
        const dateOrNa = (date: Date | null) => (date ? formatDate(date) : 'N/A');
        return `${formState.company ? formState.company.name + ' - ' : ''} ${dateOrNa(
            formState.startDate
        )} - ${dateOrNa(formState.endDate)}`;
    }, [formState]);

    const backButtonTooltipRef = useTooltipRef('Go back to ' + goBackTo, 'right');

    return (
        <div className={styles.container}>
            <header className={styles.header}>
                <IconButton onClick={onGoBack} ref={backButtonTooltipRef}>
                    <ArrowBackIcon />
                </IconButton>
                <h2>{title}</h2>
                <Button outlined onClick={onCancel}>
                    Cancel
                </Button>
                <Button
                    outlined
                    disabled={!isFormValid || !isFormDirty || isSaving}
                    onClick={saveAsync}
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
                                    selectedPositionId={formState.externalCompanyRepPositionId}
                                    onSelect={formFieldSetter('externalCompanyRepPositionId')}
                                />
                                <NewPositionSidesheet
                                    repType="company-rep"
                                    contract={formState}
                                    existingPosition={formState.externalCompanyRep}
                                    onComplete={formFieldSetter('externalCompanyRepPositionId')}
                                />
                            </div>
                        </div>
                        <div className={styles.row}>
                            <div className={styles.field}>
                                <ContractPositionPicker
                                    label="External Contract responsible"
                                    contractId={formState.id || undefined}
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
                                    existingPosition={formState.externalContractResponsible}
                                    onComplete={formFieldSetter(
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
