import * as React from 'react';
import {
    ModalSideSheet,
    Button,
    TextInput,
    DatePicker,
    PersonPicker,
    CheckBox,
    AddIcon,
    EditIcon,
} from '@equinor/fusion-components';
import useCreatePositionForm from '../hooks/useCreatePositionForm';
import * as styles from '../styles.less';
import { PersonDetails, Position } from '@equinor/fusion';
import BasePositionPicker from './BasePositionPicker';
import Contract from '../../../../../models/contract';
import usePositionPersister from '../hooks/usePositionPersister';
import CreatePositionRequest from '../../../../../models/createPositionRequest';

type NewPositionSidesheetProps = {
    contract: Contract;
    onComplete: (positionId: string) => void;
    repType: 'company-rep' | 'contract-responsible';
    existingPosition: Position | null;
};

const createRequestFromPosition = (position: Position | null) => {
    if (!position || ['ext-comp-rep', 'ext-contr-resp'].indexOf(position.externalId) === -1) {
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

const NewPositionSidesheet: React.FC<NewPositionSidesheetProps> = ({
    repType,
    contract,
    onComplete,
    existingPosition,
}) => {
    const [isShowing, setIsShowing] = React.useState(false);

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

    const show = React.useCallback(() => setIsShowing(true), []);
    const onClose = React.useCallback(() => {
        setIsShowing(false);
        resetForm();
    }, []);

    const onSave = usePositionPersister(formState, contract, repType, onComplete, onClose);

    return (
        <>
            {editPosition ? (
                <div className={styles.row}>
                    <Button frameless onClick={show}>
                        <EditIcon /> Edit {editPosition.name}
                    </Button>
                </div>
            ) : (
                <div className={styles.row}>
                    <span>If you can't find your position, try to </span>
                    <Button frameless onClick={show}>
                        <AddIcon /> Add new position
                    </Button>
                </div>
            )}
            <ModalSideSheet
                show={isShowing}
                onClose={onClose}
                size="large"
                header="Add new position to contract"
                headerIcons={[
                    <Button
                        key="save-button"
                        onClick={onSave}
                        disabled={!isFormValid || !isFormDirty}
                    >
                        Save
                    </Button>,
                ]}
            >
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
                </div>
            </ModalSideSheet>
        </>
    );
};

export default NewPositionSidesheet;
