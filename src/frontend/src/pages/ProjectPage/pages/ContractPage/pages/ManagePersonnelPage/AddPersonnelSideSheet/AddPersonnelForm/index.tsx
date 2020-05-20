import * as React from 'react';
import * as styles from '../styles.less';
import PersonnelLine from '../models/PersonnelLine';
import { AddPersonnelFormHead } from './components/AddPersonnelFormHeader';
import { AddPersonnelFormLine } from './components/AddPersonnelFormLine';
import useBasePositions from '../../../../../../../../hooks/useBasePositions';

type AddPersonnelFormProps = {
    formState: PersonnelLine[];
    setFormState: (state: PersonnelLine[]) => void;
    saveInProgress: boolean;
    setPersonState: (person: PersonnelLine) => void;
    onDeletePerson: (person: PersonnelLine) => void;
};

const AddPersonnelForm: React.FC<AddPersonnelFormProps> = ({
    formState,
    setFormState,
    saveInProgress,
    setPersonState,
    onDeletePerson,
}) => {
    const { basePositions, isFetchingBasePositions } = useBasePositions();

    const renderFormHeader = React.useMemo(
        () => <AddPersonnelFormHead formState={formState} setFormState={setFormState} />,
        [formState]
    );

    const renderFormBody = React.useMemo(
        () => (
            <tbody>
                {formState.map((person) => (
                    <AddPersonnelFormLine
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
        [formState, saveInProgress]
    );

    return (
        <table className={styles.tableBody}>
            {renderFormHeader}
            {renderFormBody}
        </table>
    );
};

export default AddPersonnelForm;
