import * as React from 'react';
import { ModalSideSheet, Button, Spinner } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';
import { BasePosition, Position } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest, createDefaultState } from './utils';
import EditableTable from '../EditableTable';
import useRequestsParentPosition from './hooks/useRequestsParentPosition';
import useForm from '../../../../../../hooks/useForm';

import useSubmitChanges from './hooks/useSubmitChanges';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import RequestProgressSidesheet from './components/RequestProgressSidesheet';
import useBasePositions from '../../../../../../hooks/useBasePositions';
import usePersonnel from '../../pages/ManagePersonnelPage/hooks/usePersonnel';
import { ReadonlyCollection } from '../../../../../../reducers/utils';

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
};

type EditRequestSideSheetProps = {
    initialRequests: PersonnelRequest[] | null;
    onClose: () => void;
};

const EditRequestSideSheet: React.FC<EditRequestSideSheetProps> = ({
    initialRequests,
    onClose,
}) => {
    const { isFetchingContract } = useContractContext();
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);
    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
        onClose();
    }, [setEditRequests]);

    React.useEffect(() => {
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
    const defaultState = React.useMemo(() => transFormRequest(editRequests, selectedPositions), [
        editRequests,
        selectedPositions,
    ]);

    const validateForm = React.useCallback((requests: EditRequest[]) => {
        return !requests.some(
            state =>
                !Boolean(
                    state.basePosition &&
                        state.positionName &&
                        state.workload &&
                        state.taskOwner &&
                        !Boolean(isNaN(+state.workload))
                )
        );
    }, []);

    const { formState, setFormState, isFormDirty, isFormValid } = useForm(
        createDefaultState,
        validateForm,
        defaultState
    );

    const { submit, pendingRequests, failedRequests, successfulRequests } = useSubmitChanges(
        formState
    );

    React.useEffect(() => {
        setFormState(
            failedRequests.filter(r => r.isEditable).map(r => r.item)
        );
    }, [failedRequests]);

    const isSubmitting = React.useMemo(() => pendingRequests.length > 0, [pendingRequests]);

    return (
        <ModalSideSheet
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
                    disabled={!(isFormDirty && isFormValid) || isSubmitting}
                    key={'save'}
                    outlined
                    onClick={submit}
                >
                    {isSubmitting ? <Spinner inline /> : 'Submit'}
                </Button>,
            ]}
        >
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
            />
            <RequestProgressSidesheet
                pendingRequests={pendingRequests}
                failedRequests={failedRequests}
                successfulRequests={successfulRequests}
                onClose={() => {}}
            />
        </ModalSideSheet>
    );
};

export default EditRequestSideSheet;
