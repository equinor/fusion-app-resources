import * as React from 'react';
import { ModalSideSheet, Button } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';
import { BasePosition, Position } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest, createDefaultState } from './utils';
import EditableTable from '../EditableTable';
import usePositionsByIds from './usePositionsByIds';

export type EditRequest = {
    id: string;
    description: string;
    positionId: string;
    basePosition: BasePosition | null;
    positionName: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: string;
    obs: string;
    person: Personnel | null;
    parentPositionId: Position | null;
};

const EditRequestSideSheet: React.FC = () => {
    const { editRequests, setEditRequests, isFetchingContract } = useContractContext();
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);

    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
    }, [setEditRequests]);
    const parentPositionsIds = React.useMemo(() => {
        if (!editRequests) {
            return [];
        }
        return editRequests.reduce((positionIds: string[], request) => {
            const parentPositionId = request.position?.instances.find(i => i.parentPositionId)
                ?.parentPositionId;
            if (parentPositionId) {
                return [...positionIds, parentPositionId];
            }
            return positionIds;
        }, []);
    }, [editRequests]);
    const {selectedPositions} = usePositionsByIds(parentPositionsIds);

    const defaultState = React.useMemo(() => transFormRequest(editRequests, selectedPositions), [editRequests, selectedPositions]);

    return (
        <ModalSideSheet
            isResizable
            header="Edit/Create requests"
            show={showSideSheet}
            size="fullscreen"
            onClose={closeSideSheet}
            headerIcons={[
                <Button disabled={false} key={'save'} outlined>
                    {'Submit'}
                </Button>,
            ]}
        >
            <EditableTable
                columns={columns}
                createDefaultState={createDefaultState}
                defaultState={defaultState}
                rowIdentifier="id"
            />
        </ModalSideSheet>
    );
};

export default EditRequestSideSheet;
