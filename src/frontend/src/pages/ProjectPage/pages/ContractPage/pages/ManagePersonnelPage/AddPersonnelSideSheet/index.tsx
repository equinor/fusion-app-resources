import * as React from 'react';
import { ModalSideSheet, Button, Spinner, AddIcon } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import Person from '../../../../../../../models/Person';
import { v1 as uuid } from 'uuid';
import * as styles from './styles.less';
import {
    useCurrentContext,
    useNotificationCenter,
} from '@equinor/fusion';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import AddPersonnelFormTextInput from './AddPersonnelFormTextInput';
import useAddPersonnelForm from '../hooks/useAddPersonnelForm';

type AddPersonnelToSideSheetProps = {
    isOpen: boolean;
    selectedPersonnel: Personnel[] | null;
    setIsOpen: (state: boolean) => void;
};

const AddPersonnelSideSheet: React.FC<AddPersonnelToSideSheetProps> = ({
    isOpen,
    setIsOpen,
    selectedPersonnel,
}) => {
    const currentContext = useCurrentContext();
    const currentContract = useContractContext();
    const notification = useNotificationCenter();
    const { formState, setFormState, isFormValid, isFormDirty } = useAddPersonnelForm(
        selectedPersonnel
    );
    const { apiClient } = useAppContext();

    const [saveInProgress, setSaveInProgress] = React.useState<boolean>(false);

    const savePersonnelChangesAsync = async () => {
        const contractId = currentContract.contract?.id;

        if (!currentContext?.id || !contractId) return;

        setSaveInProgress(true);
        await Promise.all(
            formState.map(
                async person =>
                    await apiClient.updatePersonnelAsync(currentContext.id, contractId, person)
            )
        )
            .then(() => {
                setSaveInProgress(false);
                setIsOpen(false);
                notification({
                    level: 'low',
                    title: 'Personnel changes saved',
                    cancelLabel: 'dismiss',
                });
            })
            .catch(e => {
                //TODO: This could probably be more helpfull.
                notification({
                    level: 'high',
                    title:
                        'Something went wrong while saving. Please try again or contact administrator',
                });
                setSaveInProgress(false);
            });
    };

    const onChange = React.useCallback(
        (changedPerson: Person) => {
            const updatedPersons = formState.map(p =>
                p.personnelId === changedPerson.personnelId ? changedPerson : p
            );
            setFormState(updatedPersons);
        },
        [formState]
    );

    const onAddPerson = React.useCallback(() => {
        setFormState([
            ...formState,
            {
                personnelId: uuid(),
                name: "",
                firstName: "",
                lastName: "",
                phoneNumber: "",
                mail: "",
                jobTitle: ""
            },
        ]);
    }, [formState]);

    return (
        <ModalSideSheet
            header="Add Person"
            show={isOpen}
            size={'fullscreen'}
            onClose={() => { setIsOpen(false) }}
            safeClose
            safeCloseTitle={`Close Add Person? Unsaved changes will be lost.`}
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
            headerIcons={[
                <Button disabled={saveInProgress} key={'AddPerson'} outlined onClick={onAddPerson}>
                    <AddIcon /> Add Person
                </Button>,
                <Button
                    disabled={!(isFormDirty && isFormValid) || saveInProgress}
                    key={'save'}
                    outlined
                    onClick={savePersonnelChangesAsync}
                >
                    {saveInProgress ? <Spinner inline /> : 'Create'}
                </Button>,
            ]}
        >
            {isOpen && (
                <div className={styles.container}>
                    <table>
                        <thead>
                            <tr>
                                <th className={styles.header}>First Name</th>
                                <th className={styles.header}>Last Name</th>
                                <th className={styles.header}>E-Mail</th>
                                <th className={styles.header}>Phone Number</th>
                            </tr>
                        </thead>
                        <tbody>
                            {formState.map(person => (
                                <tr
                                    key={`person${person.personnelId}`}
                                >
                                    <td className={styles.tableRowCell}>
                                        <AddPersonnelFormTextInput
                                            key={`firstname${person.personnelId}`}
                                            disabled={saveInProgress}
                                            item={person}
                                            onChange={onChange}
                                            field={'firstName'}
                                        />
                                    </td>
                                    <td className={styles.tableRowCell}>
                                        <AddPersonnelFormTextInput
                                            key={`lastname${person.personnelId}`}
                                            disabled={saveInProgress}
                                            item={person}
                                            onChange={onChange}
                                            field={'lastName'}
                                        />
                                    </td>
                                    <td className={styles.tableRowCell}>
                                        <AddPersonnelFormTextInput
                                            key={`mail${person.personnelId}`}
                                            disabled={saveInProgress}
                                            item={person}
                                            onChange={onChange}
                                            field={'mail'}
                                        />
                                    </td>
                                    <td className={styles.tableRowCell}>
                                        <AddPersonnelFormTextInput
                                            key={`phoneNumber${person.personnelId}`}
                                            disabled={saveInProgress}
                                            item={person}
                                            onChange={onChange}
                                            field={'phoneNumber'}
                                        />
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )
            }
        </ModalSideSheet >
    );
};

export default AddPersonnelSideSheet;
