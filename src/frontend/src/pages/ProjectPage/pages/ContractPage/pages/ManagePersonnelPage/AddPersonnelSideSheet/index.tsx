import * as React from 'react';
import {
    ModalSideSheet,
    Button,
    Spinner,
    AddIcon,
    useTooltipRef,
    styling,
    usePopoverRef,
    MoreIcon,
} from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import { v1 as uuid } from 'uuid';
import * as styles from './styles.less';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import AddPersonnelFormTextInput from './AddPersonnelFormTextInput';
import useAddPersonnelForm from '../hooks/useAddPersonnelForm';
import AddPersonnelFormDisciplinesDropDown from './AddPersonnelFormDisciplinesDropDown';
import ManagePersonnelToolBar, { IconButtonProps } from '../components/ManagePersonnelToolBar';
import useBasePositions from '../../../../../../../hooks/useBasePositions';
import SelectionCell from '../components/SelectionCell';

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
    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const notification = useNotificationCenter();
    const [saveInProgress, setSaveInProgress] = React.useState<boolean>(false);
    const [selectedItems, setSelectedItems] = React.useState<Personnel[]>([]);
    const { formState, setFormState, isFormValid, isFormDirty } = useAddPersonnelForm(
        selectedPersonnel
    );

    const { basePositions, isFetchingBasePositions } = useBasePositions();

    const savePersonnelChangesAsync = async () => {
        const contractId = contract?.id;

        if (!currentContext?.id || !contractId) return;

        setSaveInProgress(true);

        try {
            const response = await Promise.all(
                formState.map(async person =>
                    person.created
                        ? await apiClient.updatePersonnelAsync(
                              currentContext.id,
                              contractId,
                              person
                          )
                        : await apiClient.createPersonnelAsync(
                              currentContext.id,
                              contractId,
                              person
                          )
                )
            );

            setSaveInProgress(false);
            setIsOpen(false);
            notification({
                level: 'low',
                title: 'Personnel changes saved',
                cancelLabel: 'dismiss',
            });

            dispatchContractAction({ verb: 'merge', collection: 'personnel', payload: response });
        } catch (e) {
            //TODO: This could probably be more helpfull.
            notification({
                level: 'high',
                title:
                    'Something went wrong while saving. Please try again or contact administrator',
            });
        }
        setSaveInProgress(false);
    };

    const onChange = React.useCallback(
        (changedPerson: Personnel) => {
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
                name: '',
                firstName: '',
                lastName: '',
                phoneNumber: '',
                mail: '',
                jobTitle: '',
                disciplines: [],
            },
        ]);
    }, [formState]);

    const onDeletePerson = React.useCallback(
        (person: Personnel) => {
            const personFound = formState.findIndex(p => p.personnelId === person.personnelId);
            if (personFound < 0) return;

            const newState = [...formState];
            newState.splice(personFound, 1);
            setFormState(newState);

            const personSelected = selectedItems.findIndex(
                p => p.personnelId === person.personnelId
            );
            if (personSelected > -1) {
                const newSelected = [...selectedItems];
                newSelected.splice(personSelected, 1);
                setSelectedItems(newSelected);
            }
        },
        [formState, selectedItems]
    );

    const addButton = React.useMemo((): IconButtonProps => {
        return { onClick: onAddPerson, disabled: saveInProgress };
    }, [saveInProgress, onAddPerson]);

    const deleteButton = React.useCallback(
        (person: Personnel): IconButtonProps => {
            if (person.created || formState.length <= 1) return { disabled: true };

            return { onClick: () => onDeletePerson(person) };
        },
        [onDeletePerson, formState]
    );

    const isAllSelected = React.useMemo(() => selectedItems.length === formState.length, [
        selectedItems,
        formState,
    ]);

    const selectableTooltipRef = useTooltipRef(
        isAllSelected ? 'Unselect all' : 'Select all',
        'above'
    );

    const onSelectAll = React.useCallback(() => {
        setSelectedItems(selectedItems.length === formState.length ? [] : formState);
    }, [formState, selectedItems]);

    const onSelect = React.useCallback(
        (item: Personnel) => {
            if (selectedItems && selectedItems.some(i => i === item)) {
                setSelectedItems(selectedItems.filter(i => i !== item));
            } else {
                setSelectedItems([...(selectedItems || []), item]);
            }
        },
        [selectedItems]
    );

    type PopOverMenuProps = {
        person: Personnel;
    };

    const PopOverMenu: React.FC<PopOverMenuProps> = ({ person }) => {
        const [popoverRef, isOpen] = usePopoverRef<HTMLDivElement>(
            <ManagePersonnelToolBar deleteButton={deleteButton(person)} />,
            {
                justify: 'center',
            }
        );

        return (
            <div ref={popoverRef}>
                <MoreIcon />
            </div>
        );
    };

    return (
        <ModalSideSheet
            header="Add Person"
            show={isOpen}
            size={'fullscreen'}
            onClose={() => {
                setIsOpen(false);
            }}
            safeClose={isFormDirty}
            safeCloseTitle={`Close Add Person? Unsaved changes will be lost.`}
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
            headerIcons={[
                <Button
                    disabled={!(isFormDirty && isFormValid) || saveInProgress}
                    key={'save'}
                    outlined
                    onClick={savePersonnelChangesAsync}
                >
                    {saveInProgress ? (
                        <>
                            <Spinner inline />
                            Saving
                        </>
                    ) : (
                        'Save'
                    )}
                </Button>,
            ]}
        >
            {isOpen && (
                <div className={styles.container}>
                    <ManagePersonnelToolBar addButton={addButton} />
                    <table>
                        <thead className={styles.tableBody}>
                            <tr className={styles.tableRow}>
                                <th className={styles.tableRowHeaderSelectionCell}>
                                    <SelectionCell
                                        isSelected={
                                            !!selectedItems &&
                                            selectedItems.length === formState.length
                                        }
                                        onChange={onSelectAll}
                                        indeterminate={
                                            !!selectedItems &&
                                            selectedItems.length > 0 &&
                                            selectedItems.length !== formState.length
                                        }
                                        ref={selectableTooltipRef}
                                    />
                                </th>
                                <th className={styles.tableRowHeaderSelectionCell}>
                                    <MoreIcon />
                                </th>
                                <th className={styles.headerRowCell}>First Name</th>
                                <th className={styles.headerRowCell}>Last Name</th>
                                <th className={styles.headerRowCell}>E-Mail</th>
                                <th className={styles.headerRowCell}>Disciplines</th>
                                <th className={styles.headerRowCell}>Phone Number</th>
                            </tr>
                        </thead>
                        <tbody className={styles.tableBody}>
                            {!isFetchingBasePositions &&
                                formState.map(person => (
                                    <tr
                                        className={styles.tableRow}
                                        key={`person${person.personnelId}`}
                                    >
                                        <td className={styles.tableRowCell}>
                                            <SelectionCell
                                                isSelected={
                                                    !!selectedItems &&
                                                    selectedItems.some(i => i === person)
                                                }
                                                onChange={() => onSelect(person)}
                                            />
                                        </td>
                                        <td className={styles.tableRowCellMenu}>
                                            <PopOverMenu person={person} />
                                        </td>
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
                                                disabled={Boolean(person.created || saveInProgress)}
                                                item={person}
                                                onChange={onChange}
                                                field={'mail'}
                                            />
                                        </td>
                                        <td className={styles.tableRowCell}>
                                            <AddPersonnelFormDisciplinesDropDown
                                                key={`disciplines${person.personnelId}`}
                                                disabled={saveInProgress}
                                                onChange={onChange}
                                                item={person}
                                                basePositions={basePositions}
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
            )}
        </ModalSideSheet>
    );
};

export default AddPersonnelSideSheet;
