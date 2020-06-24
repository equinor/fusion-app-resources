import * as React from 'react';
import * as styles from '../../styles.less';
import Personnel from '../../../../../../../../../models/Personnel';
import SelectionCell from '../../../components/SelectionCell';
import AddPersonnelFormTextInput from './AddPersonnelFormTextInput';
import { SkeletonBar } from '@equinor/fusion-components';
import AddPersonnelFormDisciplinesDropDown from './AddPersonnelFormDisciplinesDropDown';
import useForm from '../../../../../../../../../hooks/useForm';
import { BasePosition } from '@equinor/fusion';
import { PopOverMenu } from './AddPersonnelFormLinePopOverMenu';
import PersonnelLine from '../../models/PersonnelLine';

type AddPersonnelFormRowProps = {
    person: PersonnelLine;
    rowNumber: number;
    setPersonState: (person: PersonnelLine) => void;
    saveInProgress: boolean;
    isFetchingBasePositions: Boolean;
    basePositions: BasePosition[];
    deletePerson: (person: PersonnelLine) => void;
};

export const AddPersonnelFormRow: React.FC<AddPersonnelFormRowProps> = ({
    person,
    setPersonState,
    saveInProgress,
    isFetchingBasePositions,
    basePositions,
    deletePerson,
    rowNumber,
}) => {
    const validateForm = React.useCallback((formState: Personnel) => {
        return Boolean(
            formState.firstName && formState.lastName && formState.phoneNumber && formState.mail
        );
    }, []);

    const { formState, formFieldSetter, setFormState } = useForm(
        () => person,
        validateForm,
        person
    );

    const onSelect = React.useCallback(() => {
        formFieldSetter('selected')(!formState?.selected);
    }, [formState]);

    React.useEffect(() => {
        setPersonState(formState);
    }, [formState]);

    return (
        <tr className={styles.tableRow} key={`person${formState.personnelId}`}>
            <td className={styles.tableRowCell}>
                <SelectionCell isSelected={Boolean(formState.selected)} onChange={onSelect} />
            </td>
            <td className={styles.tableRowCellMenu}>
                <PopOverMenu person={formState} onDeletePerson={deletePerson} />
            </td>
            <td className={styles.tableRowCell} style={{ textAlign: 'center' }}>
                <p>{rowNumber}</p>
            </td>
            <td className={styles.tableRowCell}>
                <AddPersonnelFormTextInput
                    key={`firstname${formState.personnelId}`}
                    disabled={saveInProgress}
                    item={formState}
                    onChange={formFieldSetter}
                    field={'firstName'}
                />
            </td>
            <td className={styles.tableRowCell}>
                <AddPersonnelFormTextInput
                    key={`lastname${formState.personnelId}`}
                    disabled={saveInProgress}
                    item={formState}
                    onChange={formFieldSetter}
                    field={'lastName'}
                />
            </td>
            <td className={styles.tableRowCell}>
                <AddPersonnelFormTextInput
                    key={`mail${formState.personnelId}`}
                    disabled={Boolean(formState.created || saveInProgress)}
                    item={formState}
                    onChange={formFieldSetter}
                    field={'mail'}
                />
            </td>

            <td className={styles.tableRowCell}>
                <AddPersonnelFormTextInput
                    key={`phoneNumber${formState.personnelId}`}
                    disabled={saveInProgress}
                    item={formState}
                    onChange={formFieldSetter}
                    field={'phoneNumber'}
                />
            </td>
            <td className={styles.tableRowCell}>
                <AddPersonnelFormTextInput
                    key={`dawinci${formState.personnelId}`}
                    disabled={saveInProgress}
                    item={formState}
                    onChange={formFieldSetter}
                    field={'dawinciCode'}
                />
            </td>
            <td className={styles.tableRowCell}>
                {isFetchingBasePositions ? (
                    <SkeletonBar />
                ) : (
                    <AddPersonnelFormDisciplinesDropDown
                        key={`disciplines${formState.personnelId}`}
                        disabled={saveInProgress}
                        onChange={setFormState}
                        item={formState}
                        basePositions={basePositions}
                    />
                )}
            </td>
        </tr>
    );
};
