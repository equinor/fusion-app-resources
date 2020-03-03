import * as React from 'react';
import {
    ModalSideSheet,
    Button,
    TextInput,
    DatePicker,
    PersonPicker,
    CheckBox,
    AddIcon,
} from '@equinor/fusion-components';
import useCreatePositionForm from '../hooks/useCreatePositionForm';
import * as styles from '../styles.less';
import { PersonDetails } from '@equinor/fusion';
import BasePositionPicker from './BasePositionPicker';
import Contract from '../../../../../models/contract';
import usePositionPersister from '../hooks/usePositionPersister';

type NewPositionSidesheetProps = {
    contract: Contract;
    onComplete: (positionId: string) => void;
    repType: 'company-rep' | 'contract-responsible';
};

const NewPositionSidesheet: React.FC<NewPositionSidesheetProps> = ({
    repType,
    contract,
    onComplete,
}) => {
    const [isShowing, setIsShowing] = React.useState(false);

    const {
        formState,
        formFieldSetter,
        setFormField,
        resetForm,
        isFormValid,
        isFormDirty,
    } = useCreatePositionForm();

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
            <div className={styles.row}>
                <span>If you can't find your position, try to </span>
                <Button frameless onClick={show}>
                    <AddIcon /> Add new position
                </Button>
            </div>
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
