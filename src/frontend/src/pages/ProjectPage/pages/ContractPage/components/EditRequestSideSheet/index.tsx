
import {
    ModalSideSheet,
    Button,
    Spinner,
    IconButton,
    HelpIcon,
    useTooltipRef,
} from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';
import { BasePosition, Position } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest, createDefaultState, createCopyState } from './utils';
import EditableTable from '../EditableTable';
import useRequestsParentPosition from './hooks/useRequestsParentPosition';
import useForm from '../../../../../../hooks/useForm';

import useSubmitChanges from './hooks/useSubmitChanges';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import RequestProgressSidesheet from './components/RequestProgressSidesheet';
import useBasePositions from '../../../../../../hooks/useBasePositions';
import usePersonnel from '../../pages/ManagePersonnelPage/hooks/usePersonnel';
import { ReadonlyCollection } from '../../../../../../reducers/utils';
import { Link } from 'react-router-dom';
import styles from './styles.less';
import { FC, useState, useMemo, useEffect, useCallback } from 'react';

export type EditRequest = {
    id: string;
    requestId: string | null;
    description: string;
    positionId: string | null;
    basePosition: BasePosition | null;
    positionName: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: string;
    obs: string;
    person: Personnel | null;
    taskOwner: Position | null;
    originalPositionId: string | null;
};

type EditRequestSideSheetProps = {
    initialRequests: PersonnelRequest[] | null;
    onClose: () => void;
};

const EditRequestSideSheet: FC<EditRequestSideSheetProps> = ({
    initialRequests,
    onClose,
}) => {
    const { isFetchingContract } = useContractContext();
    const [editRequests, setEditRequests] = useState<PersonnelRequest[] | null>(null);
    const showSideSheet = useMemo(() => editRequests !== null, [editRequests]);
    const helpIconRef = useTooltipRef('Help page', 'below');

    useEffect(() => {
        if (initialRequests) {
            setEditRequests(initialRequests);
        }
    }, [initialRequests]);

    const { selectedPositions, isFetchingPositions } = useRequestsParentPosition(editRequests);

    const { basePositions, isFetchingBasePositions, basePositionsError } = useBasePositions();
    const basePositionState: ReadonlyCollection<BasePosition> = {
        data: basePositions,
        isFetching: !!isFetchingBasePositions,
        error: basePositionsError,
    };

    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel();
    const personnelState: ReadonlyCollection<Personnel> = {
        data: personnel,
        isFetching: isFetchingPersonnel,
        error: personnelError,
    };

    const defaultState = useMemo(() => transFormRequest(editRequests, selectedPositions), [
        editRequests,
        selectedPositions,
    ]);

    const validateForm = useCallback((requests: EditRequest[]) => {
        return !requests.some(
            (state) =>
                !Boolean(
                    state.basePosition &&
                        state.positionName &&
                        state.workload &&
                        state.appliesFrom &&
                        state.appliesTo &&
                        state.person &&
                        !Boolean(isNaN(+state.workload.split('%')[0]))
                )
        );
    }, []);

    const { formState, setFormState, isFormDirty, isFormValid, resetForm } = useForm(
        createDefaultState,
        validateForm,
        defaultState
    );

    const {
        submit,
        reset,
        pendingRequests,
        failedRequests,
        successfulRequests,
        removeFailedRequest,
    } = useSubmitChanges(formState);

    const closeSideSheet = useCallback(() => {
        reset();
        setEditRequests(null);
        resetForm();
        onClose();
    }, [setEditRequests, resetForm]);

    const onProgressSidesheetClose = useCallback(() => {
        const editableFailedRequests = failedRequests.filter((r) => r.isEditable);
        if (editableFailedRequests.length > 0) {
            setFormState(editableFailedRequests.map((r) => r.item));
            return;
        }

        closeSideSheet();
    }, [failedRequests, closeSideSheet]);

    const isSubmitting = useMemo(() => pendingRequests.length > 0, [pendingRequests]);

    const onItemChange = useCallback((item: EditRequest, key: keyof EditRequest) => {
        switch (key) {
            case 'basePosition':
                if (item.basePosition && !item.positionName) {
                    return {
                        ...item,
                        positionName: item.basePosition.name,
                    };
                }
        }

        return item;
    }, []);

    return (
        <ModalSideSheet
            id="edit-request-sidesheet"
            isResizable
            header="Edit/Create requests"
            show={showSideSheet}
            size="fullscreen"
            onClose={closeSideSheet}
            safeClose={isFormDirty}
            safeCloseTitle={`Close Edit/Request personnel? Unsaved changes will be lost.`}
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
            headerIcons={[
                <Button
                    id="submit-btn"
                    disabled={!(isFormDirty && isFormValid) || isSubmitting}
                    key={'save'}
                    outlined
                    onClick={submit}
                >
                    {isSubmitting ? <Spinner inline /> : 'Submit'}
                </Button>,
            ]}
        >
            <div className={styles.helpButton}>
                <Link data-cy="help-btn" target="_blank" to="/help">
                    <IconButton id="help-btn" ref={helpIconRef}>
                        <HelpIcon />
                    </IconButton>
                </Link>
            </div>
            <EditableTable
                columns={columns}
                createDefaultState={createDefaultState}
                formState={formState}
                setFormState={setFormState}
                rowIdentifier="id"
                isFetching={isFetchingPositions || isFetchingContract}
                componentState={{
                    personnel: personnelState,
                    basePositions: basePositionState,
                }}
                createCopyState={createCopyState}
                onItemChange={onItemChange}
            />
            <RequestProgressSidesheet
                data-cy="request-progress-sidesheet"
                pendingRequests={pendingRequests}
                failedRequests={failedRequests}
                successfulRequests={successfulRequests}
                onClose={onProgressSidesheetClose}
                onRemoveFailedRequest={removeFailedRequest}
            />
        </ModalSideSheet>
    );
};

export default EditRequestSideSheet;
