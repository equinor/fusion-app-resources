import * as React from 'react';
import { Position, PersonDetails } from '@equinor/fusion';
import Contract from '../../../../../models/contract';
import CreatePositionRequest from '../../../../../models/createPositionRequest';
import useCreatePositionForm from '../hooks/useCreatePositionForm';
import usePositionPersister from '../hooks/usePositionPersister';
import * as styles from '../styles.less';
import BasePositionPicker from './BasePositionPicker';
import { TextInput, DatePicker, PersonPicker, Button, Spinner } from '@equinor/fusion-components';

type ExternalPositionSidesheetProps = {
    contract: Contract;
    onComplete: (position: Position) => void;
    repType: 'company-rep' | 'contract-responsible';
    existingPosition: Position | null;
    onClose: () => void;
};

const createRequestFromPosition = (position: Position | null) => {
    if (
        !position ||
        !position.externalId ||
        !['ext-comp-rep', 'ext-contr-resp'].includes(position.externalId)
    ) {
        return null;
    }

    const now = new Date();
    const instance = position.instances.find(i => i.appliesFrom <= now && i.appliesTo >= now);

    const request: CreatePositionRequest = {
        basePosition: position.basePosition,
        name: position.name,
        appliesFrom: instance?.appliesFrom || null,
        appliesTo: instance?.appliesTo || null,
        assignedPerson: instance?.assignedPerson || null,
        workload: instance?.workload || 0,
    };

    return request;
};

const ExternalPositionSidesheet: React.FC<ExternalPositionSidesheetProps> = ({
    contract,
    onComplete,
    repType,
    existingPosition,
    onClose,
}) => {
    const editPosition = React.useMemo(() => createRequestFromPosition(existingPosition), [
        existingPosition,
    ]);

    const {
        formState,
        formFieldSetter,
        setFormField,
        resetForm,
        isFormValid,
        isFormDirty,
    } = useCreatePositionForm(editPosition);

    const [selectedPerson, setSelectedPerson] = React.useState<PersonDetails | null>(null);
    const onPersonSelect = React.useCallback(
        (person: PersonDetails) => {
            setSelectedPerson(person);
            setFormField('assignedPerson', {
                azureUniqueId: person.azureUniqueId,
                mail: person.mail,
            });
        },
        [setFormField]
    );

    React.useEffect(() => {
        if (
            formState.assignedPerson?.azureUniqueId &&
            formState.assignedPerson.azureUniqueId !== selectedPerson?.azureUniqueId
        ) {
            setSelectedPerson(formState.assignedPerson as PersonDetails);
        }
    }, [formState.assignedPerson]);

    const closeHandler = React.useCallback(() => {
        onClose();
        resetForm();
    }, []);

    const { saveAsync, isSaving } = usePositionPersister(
        formState,
        contract,
        repType,
        onComplete,
        closeHandler
    );

    return (
        <div className={styles.sideSheetContainer}>
            <div className={styles.row}>
                <div className={styles.field}>
                    <BasePositionPicker
                        selectedBasePositionId={formState.basePosition?.id}
                        onSelect={formFieldSetter('basePosition')}
                    />
                </div>
                <div className={styles.field}>
                    <TextInput
                        label="Custom position title"
                        value={formState.name}
                        onChange={formFieldSetter('name')}
                    />
                </div>
            </div>

            <div className={styles.row}>
                <div className={styles.field}>
                    <DatePicker
                        label="From date"
                        selectedDate={formState.appliesFrom}
                        onChange={formFieldSetter('appliesFrom')}
                    />
                </div>
                <div className={styles.field}>
                    <DatePicker
                        label="To date"
                        selectedDate={formState.appliesTo}
                        onChange={formFieldSetter('appliesTo')}
                    />
                </div>
            </div>

            <div className={styles.row}>
                <div className={styles.field}>
                    <PersonPicker
                        label="Person"
                        selectedPerson={selectedPerson}
                        initialPerson={selectedPerson}
                        onSelect={onPersonSelect}
                    />
                </div>
                <div className={styles.field}>
                    <TextInput
                        label="Workload (%)"
                        value={formState.workload.toString()}
                        onChange={value => setFormField('workload', parseInt(value, 10))}
                    />
                </div>
            </div>
            <div className={styles.row}>
                <Button
                    key="save-button"
                    onClick={saveAsync}
                    disabled={!isFormValid || !isFormDirty || isSaving}
                >
                    {isSaving ? (
                        <>
                            <Spinner inline size={16} /> Saving
                        </>
                    ) : (
                        'Save'
                    )}
                </Button>
            </div>
        </div>
    );
};

export default ExternalPositionSidesheet;
