import * as React from 'react';
import { ModalSideSheet, Button, Spinner, ArrowBackIcon } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import { v1 as uuid } from 'uuid';
import * as styles from './styles.less';
import {
    useCurrentContext,
    HttpClientRequestFailedError,
    FusionApiHttpErrorResponse,
} from '@equinor/fusion';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import useAddPersonnelForm from '../hooks/useAddPersonnelForm';
import ManagePersonnelToolBar, { IconButtonProps } from '../components/ManagePersonnelToolBar';
import RequestProgressSidesheet, {
    FailedRequest,
    SuccessfulRequest,
} from '../../../../../../../components/RequestProgressSidesheet';
import PersonnelRequest from './PersonnelRequest';
import AddPersonnelForm from './AddPersonnelForm';
import PersonnelLine from './models/PersonnelLine';
import useScrollToTop from '../../../../../../../hooks/useScrollToTop';
import ScrollUpFab from '../../../../../../../components/ScrollUpFab';

type AddPersonnelToSideSheetProps = {
    isOpen: boolean;
    selectedPersonnel: Personnel[] | null;
    setIsOpen: (state: boolean) => void;
    excelImport: boolean;
};

const AddPersonnelSideSheet: React.FC<AddPersonnelToSideSheetProps> = ({
    isOpen,
    setIsOpen,
    selectedPersonnel,
    excelImport,
}) => {
    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const [triggerSelectionUpdate, setTriggerSelectionUpdate] = React.useState(false);
    const { formState, setFormState, isFormValid, isFormDirty, resetForm } = useAddPersonnelForm(
        selectedPersonnel
    );

    const [pendingRequests, setPendingRequests] = React.useState<Personnel[]>([]);
    const [failedRequests, setFailedRequests] = React.useState<FailedRequest<Personnel>[]>([]);
    const [successfulRequests, setSuccessfullRequests] = React.useState<
        SuccessfulRequest<Personnel, Personnel>[]
    >([]);

    const isDirty = React.useMemo(() => {
        return excelImport ? excelImport : isFormDirty;
    }, [excelImport, isFormDirty]);

    React.useEffect(() => {
        if (failedRequests.length) {
            setFormState(failedRequests.filter((r) => r.isEditable).map((r) => r.item));
        }
    }, [failedRequests]);

    const savePersonnelAsync = React.useCallback(
        async (person: Personnel, contextId: string, contractId: string) => {
            try {
                setPendingRequests((r) => [...r, person]);
                const response = person.created
                    ? await apiClient.updatePersonnelAsync(contextId, contractId, person)
                    : await apiClient.createPersonnelAsync(contextId, contractId, person);

                setSuccessfullRequests((r) => [...r, { item: person, response }]);

                dispatchContractAction({
                    collection: 'personnel',
                    verb: 'merge',
                    payload: [response],
                });
            } catch (error) {
                if (error instanceof HttpClientRequestFailedError) {
                    const requestError = error as HttpClientRequestFailedError<
                        FusionApiHttpErrorResponse
                    >;
                    setFailedRequests((f) => [
                        ...f,
                        {
                            error: requestError.response,
                            item: person,
                            isEditable:
                                requestError.statusCode <= 500 &&
                                requestError.statusCode !== 424 &&
                                requestError.statusCode !== 408,
                        },
                    ]);
                } else {
                    setFailedRequests((f) => [
                        ...f,
                        {
                            error,
                            item: person,
                            isEditable: false,
                        },
                    ]);
                }
            } finally {
                setPendingRequests((r) => r.filter((x) => x !== person));
            }
        },
        [apiClient]
    );

    const savePersonnelChangesAsync = React.useCallback(async () => {
        const contractId = contract?.id;

        if (!currentContext?.id || !contractId) return;

        setPendingRequests([]);
        setFailedRequests([]);
        setSuccessfullRequests([]);

        formState.forEach((person) => savePersonnelAsync(person, currentContext.id, contractId));
    }, [contract, formState, currentContext, savePersonnelAsync]);

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

    const setPersonState = React.useCallback((person: PersonnelLine) => {
        setFormState((previousState) =>
            previousState.map((p) => (p.personnelId === person.personnelId ? person : p))
        );
    }, []);

    const setSelectionState = React.useCallback((setAll: boolean) => {
        setFormState((previousState) =>
            previousState.map((p) => {
                return { ...p, selected: setAll };
            })
        );
        setTriggerSelectionUpdate((previousState) => !previousState);
    }, []);

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

    const saveInProgress = React.useMemo(() => pendingRequests.length > 0, [pendingRequests]);

    const addButton = React.useMemo((): IconButtonProps => {
        return { onClick: onAddPerson, disabled: saveInProgress };
    }, [saveInProgress, onAddPerson]);

    const saveButton = React.useMemo(() => {
        return (
            <Button
                disabled={!(isDirty && isFormValid) || saveInProgress}
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
            </Button>
        );
    }, [isDirty, isFormValid, saveInProgress, savePersonnelChangesAsync]);

    const closeSidesheet = React.useCallback(() => {
        resetForm();
        setIsOpen(false);
    }, [setIsOpen]);

    const onProgressSidesheetClose = React.useCallback(() => {
        const editableFailedRequests = failedRequests.filter((r) => r.isEditable);
        if (editableFailedRequests.length > 0) {
            setFormState(editableFailedRequests.map((r) => r.item));
            return;
        }

        closeSidesheet();
    }, [failedRequests, closeSidesheet]);

    const onRemoveFailedRequest = React.useCallback((request: FailedRequest<Personnel>) => {
        setFailedRequests((fr) => fr.filter((r) => r !== request));
    }, []);

    const { scrollRef, scrollToTop, hasScrolled } = useScrollToTop<HTMLDivElement>(500);

    return (
        <ModalSideSheet
            header="Add Person"
            show={isOpen}
            size={'fullscreen'}
            onClose={closeSidesheet}
            safeClose={isDirty}
            safeCloseTitle={`Close Add Person? Unsaved changes will be lost.`}
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
            headerIcons={[saveButton]}
        >
            {hasScrolled && (
                <ScrollUpFab onClick={scrollToTop}>
                    <ArrowBackIcon />
                </ScrollUpFab>
            )}

            <div ref={scrollRef} className={styles.container}>
                <ManagePersonnelToolBar addButton={addButton} />
                <AddPersonnelForm
                    formState={formState}
                    setSelectionState={setSelectionState}
                    saveInProgress={saveInProgress}
                    setPersonState={setPersonState}
                    onDeletePerson={onDeletePerson}
                    triggerSelectionUpdate={triggerSelectionUpdate}
                />
            </div>
            <RequestProgressSidesheet
                failedRequests={failedRequests}
                successfulRequests={successfulRequests}
                pendingRequests={pendingRequests}
                onClose={onProgressSidesheetClose}
                onRemoveFailedRequest={onRemoveFailedRequest}
                renderRequest={({ request }) => <PersonnelRequest person={request} />}
            />
        </ModalSideSheet>
    );
};

export default AddPersonnelSideSheet;
