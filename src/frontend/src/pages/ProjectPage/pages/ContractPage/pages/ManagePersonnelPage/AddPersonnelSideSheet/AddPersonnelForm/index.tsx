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
    isFormValid: boolean;
    isFormDirty: boolean;
};

const AddPersonnelForm: React.FC<AddPersonnelFormProps> = ({
    formState,
    setFormState,
    saveInProgress,
    isFormDirty,
    isFormValid,
}) => {
    const { basePositions, isFetchingBasePositions } = useBasePositions();
    const [selectAll, setSelectAll] = React.useState(false);

    const setPersonState = React.useCallback(
        (person: PersonnelLine) => {
            const updatedPersons = formState.map((p) =>
                p.personnelId === person.personnelId ? person : p
            );
            setFormState(updatedPersons);
        },
        [formState]
    );

    const onDeletePerson = React.useCallback(
        (person: PersonnelLine) => {
            const personFound = formState.findIndex((p) => p.personnelId === person.personnelId);
            if (personFound < 0) return;

            const newState = [...formState];
            newState.splice(personFound, 1);
            setFormState(newState);
        },
        [formState]
    );

    const renderFormHeader = React.useMemo(
        () => (
            <AddPersonnelFormHead
                formState={formState}
                setFormState={setFormState}
                setSelectAll={setSelectAll}
            />
        ),
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
        [formState.length, selectAll]
    );

    return (
        <table className={styles.tableBody}>
            {renderFormHeader}
            {renderFormBody}
        </table>
    );
};

export default AddPersonnelForm;
