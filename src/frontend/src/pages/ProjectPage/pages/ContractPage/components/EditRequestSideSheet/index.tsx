import * as React from 'react';
import { ModalSideSheet, Button } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';
import { BasePosition, Position } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest, createDefaultState } from './utils';
import EditableTable from '../EditableTable';
import useRequestsParentPosition from './useRequestsParentPosition';
import useForm from '../../../../../../hooks/useForm';

export type EditRequest = {
    id: string;
    requestId: string | null;
    description: string;
    positionId: string;
    basePosition: BasePosition | null;
    positionName: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: string;
    obs: string;
    person: Personnel | null;
    parentPosition: Position | null;
};

const EditRequestSideSheet: React.FC = () => {
    const { editRequests, setEditRequests, isFetchingContract } = useContractContext();
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);
    const [isSubmitting, setIsSubmitting] = React.useState<boolean>(false);
    const [newRequests, setNewRequests] = React.useState<EditRequest[]>([]);

    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
    }, [setEditRequests]);

    const { selectedPositions, isFetchingPositions } = useRequestsParentPosition(editRequests);

    const defaultState = React.useMemo(() => transFormRequest(editRequests, selectedPositions), [
        editRequests,
        selectedPositions,
    ]);

 

    const validateForm = React.useCallback((formState: EditRequest[]) => {
        return !formState.some(
            state =>
                !Boolean(
                    state.basePosition &&
                        state.parentPosition &&
                        state.positionName &&
                        state.workload &&
                        state.person &&
                        !Boolean(isNaN(+state.workload))
                )
        );
    }, []);

    const { formState, setFormState, isFormDirty, isFormValid } = useForm(
        createDefaultState,
        validateForm,
        defaultState
    );

    const submitChangesAsync = React.useCallback(async () => {
        
    }, []);

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
                >
                    {'Submit'}
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
            />
        </ModalSideSheet>
    );
};

export default EditRequestSideSheet;
