
import styles from '../styles.less';
import PersonnelLine from '../models/PersonnelLine';
import { AddPersonnelFormHead } from './components/AddPersonnelFormHeader';
import { AddPersonnelFormRow } from './components/AddPersonnelFormRow';
import useBasePositions from '../../../../../../../../hooks/useBasePositions';
import { FC, useMemo } from 'react';

type AddPersonnelFormProps = {
    formState: PersonnelLine[];
    saveInProgress: boolean;
    triggerSelectionUpdate: boolean;
    setSelectionState: (setAll: boolean) => void;
    setPersonState: (person: PersonnelLine) => void;
    onDeletePerson: (person: PersonnelLine) => void;
};

const AddPersonnelForm: FC<AddPersonnelFormProps> = ({
    formState,
    saveInProgress,
    setPersonState,
    onDeletePerson,
    setSelectionState,
    triggerSelectionUpdate,
}) => {
    const { basePositions, isFetchingBasePositions } = useBasePositions();

    const renderFormHeader = useMemo(
        () => <AddPersonnelFormHead formState={formState} setSelectionState={setSelectionState} />,
        [formState]
    );

    const renderFormBody = useMemo(
        () => (
            <tbody>
                {formState.map((person, i) => (
                    <AddPersonnelFormRow
                        key={`PersonnelLine${person.personnelId}`}
                        person={person}
                        rowNumber={i + 1}
                        setPersonState={setPersonState}
                        saveInProgress={saveInProgress}
                        isFetchingBasePositions={isFetchingBasePositions}
                        basePositions={basePositions}
                        deletePerson={onDeletePerson}
                    />
                ))}
            </tbody>
        ),
        [
            formState.length,
            triggerSelectionUpdate,
            saveInProgress,
            basePositions,
            isFetchingBasePositions,
        ]
    );

    return (
        <table data-cy="add-person-table" className={styles.tableBody}>
            {renderFormHeader}
            {renderFormBody}
        </table>
    );
};

export default AddPersonnelForm;
