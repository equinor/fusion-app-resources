import * as React from 'react';
import { ModalSideSheet, Button } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';
import { BasePosition } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest, createDefaultState } from './utils';
import EditableTable from '../EditableTable';

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
};

const EditRequestSideSheet: React.FC = () => {
    const { editRequests, setEditRequests, isFetchingContract } = useContractContext();
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);

    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
    }, [setEditRequests]);
    const defaultState = React.useMemo(() => transFormRequest(editRequests), [editRequests]);

    return (
        <ModalSideSheet
            isResizable
            header="Edit/Create requests"
            show={showSideSheet}
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
