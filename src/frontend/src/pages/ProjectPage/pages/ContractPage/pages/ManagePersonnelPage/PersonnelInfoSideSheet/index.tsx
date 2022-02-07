import {
    ModalSideSheet,
    Tabs,
    Tab,
    IconButton,
    EditIcon,
    DoneIcon,
    Spinner,
} from '@equinor/fusion-components';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import Personnel from '../../../../../../../models/Personnel';

import useForm from '../../../../../../../hooks/useForm';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import styles from './styles.less';
import EditablePositionDetails from '../../../components/EditablePositionDetails';
import PersonPositionsDetails from '../../../components/PersonPositionsDetails';
import { FC, useState, useCallback, useMemo } from 'react';
import DeletedAccountInfo from './DeletedAccountInfo';

type PersonnelInfoSideSheetProps = {
    isOpen: boolean;
    person: Personnel;
    setIsOpen: (state: boolean) => void;
};

const PersonnelInfoSideSheet: FC<PersonnelInfoSideSheetProps> = ({ isOpen, person, setIsOpen }) => {
    const [activeTabKey, setActiveTabKey] = useState<string>('general');
    const [editMode, setEditMode] = useState<boolean>(false);
    const [isSaving, setIsSaving] = useState<boolean>(false);
    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();

    const notification = useNotificationCenter();

    const createDefaultState = () => {
        return { ...person };
    };

    const validatePerson = (formState: Personnel) => {
        return Boolean(
            formState.firstName?.length &&
                formState.lastName?.length &&
                formState.phoneNumber?.length
        );
    };

    const { formState, isFormValid, formFieldSetter, isFormDirty } = useForm<Personnel>(
        createDefaultState,
        validatePerson
    );

    const savePersonChangesAsync = useCallback(async () => {
        const contractId = contract?.id;
        if (!currentContext?.id || !contractId) return;

        if (!isFormDirty) {
            notification({
                level: 'low',
                title: 'No changes made',
                cancelLabel: 'dismiss',
            });
            setEditMode(false);
            return;
        }

        try {
            setIsSaving(true);
            const response = await apiClient.updatePersonnelAsync(
                currentContext.id,
                contractId,
                formState
            );

            notification({
                level: 'low',
                title: 'Personnel changes saved',
                cancelLabel: 'dismiss',
            });

            dispatchContractAction({ verb: 'merge', collection: 'personnel', payload: [response] });
            setIsSaving(false);
            setEditMode(false);
        } catch (e) {
            setIsSaving(false);
            notification({
                level: 'high',
                title: 'Something went wrong while saving. Please try again or contact administrator',
            });
        }
    }, [formState, isFormValid, isFormDirty]);

    const headerIcons = useMemo(() => {
        if (activeTabKey !== 'general') return [];
        return editMode
            ? [
                  <IconButton
                      key="DoneButton"
                      disabled={isSaving || !isFormValid}
                      onClick={savePersonChangesAsync}
                  >
                      {isSaving ? <Spinner inline /> : <DoneIcon />}
                  </IconButton>,
              ]
            : [
                  <IconButton key="EditButton" onClick={() => setEditMode(true)}>
                      <EditIcon />
                  </IconButton>,
              ];
    }, [editMode, activeTabKey, savePersonChangesAsync, isFormValid, isSaving]);

    return (
        <ModalSideSheet
            header={`${person.firstName} ${person.lastName}`}
            show={isOpen}
            size={'large'}
            safeClose={editMode && isFormDirty}
            safeCloseTitle={`Close Personnel Edit? Unsaved changes will be lost.`}
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
            headerIcons={headerIcons}
            onClose={() => {
                setEditMode(false);
                setIsOpen(false);
            }}
        >
            <div className={styles.container}>
                {person.isDeleted && <DeletedAccountInfo person={person} />}
                <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                    <Tab tabKey="general" title="General">
                        <div className={styles.tabContainer}>
                            <EditablePositionDetails
                                person={formState}
                                edit={editMode}
                                setField={formFieldSetter}
                            />
                        </div>
                    </Tab>
                    <Tab disabled={editMode} tabKey="positions" title="Positions">
                        <div className={styles.tabContainer}>
                            <PersonPositionsDetails person={person} />
                        </div>
                    </Tab>
                </Tabs>
            </div>
        </ModalSideSheet>
    );
};

export default PersonnelInfoSideSheet;
