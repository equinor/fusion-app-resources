import * as React from 'react';
import * as styles from '../styles.less';
import PersonnelLine from '../models/PersonnelLine';
import { AddPersonnelFormHead } from './components/AddPersonnelFormHeader';
import { AddPersonnelFormRow } from './components/AddPersonnelFormRow';
import useBasePositions from '../../../../../../../../hooks/useBasePositions';

type AddPersonnelFormProps = {
    formState: PersonnelLine[];
    saveInProgress: boolean;
    triggerSelectionUpdate: boolean;
    setSelectionState: (setAll: boolean) => void;
    setPersonState: (person: PersonnelLine) => void;
    onDeletePerson: (person: PersonnelLine) => void;
};

const AddPersonnelForm: React.FC<AddPersonnelFormProps> = ({
    formState,
    saveInProgress,
    setPersonState,
    onDeletePerson,
    setSelectionState,
    triggerSelectionUpdate,
}) => {
    const { basePositions, isFetchingBasePositions } = useBasePositions();

    const renderFormHeader = React.useMemo(
        () => <AddPersonnelFormHead formState={formState} setSelectionState={setSelectionState} />,
        [formState]
    );

    const renderFormBody = React.useMemo(
        () => (
            <tbody>
                {formState.map((person) => (
                    <AddPersonnelFormRow
                        key={`PersonnelLine${person.personnelId}`}
                        person={person}
                        setPersonState={setPersonState}
                        saveInProgress={saveInProgress}
                        isFetchingBasePositions={isFetchingBasePositions}
                        basePositions={basePositions}
                        deletePerson={onDeletePerson}
                    />
                ))}
            </tbody>
        ),
        [formState.length, triggerSelectionUpdate, saveInProgress]
    );

    return (
        <table className={styles.tableBody}>
            {renderFormHeader}
            {renderFormBody}
        </table>
    );
};

export default AddPersonnelForm;
