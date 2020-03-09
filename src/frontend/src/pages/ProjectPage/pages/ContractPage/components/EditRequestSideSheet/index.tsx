import * as React from 'react';
import { ModalSideSheet, Button, DataTable } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './cols';
import useCreateRequestForm from './hooks/useCreateRequestForm';
import { BasePosition } from '@equinor/fusion';
import Personnel from '../../../../../../models/Personnel';
import { transFormRequest } from './utils';
import EditableTable from '../EditableTable';
import { v1 as uuid } from 'uuid';

export type EditRequest = {
    id: string,
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

const createDefaultState = (): EditRequest[] => [
    {
        id: uuid(),
        description: '',
        positionId: '',
        basePosition: null,
        positionName: '',
        appliesFrom: null,
        appliesTo: null,
        workload: '',
        obs: '',
        person: null,
    },
];

const EditRequestSideSheet: React.FC = () => {
    const { editRequests, setEditRequests, isFetchingContract } = useContractContext();
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);

    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
    }, [setEditRequests]);
    const defaultState = React.useMemo(() => transFormRequest(editRequests), [editRequests]);
    const {
        formState,
        resetForm,
        formFieldSetter,
        setFormField,
        isFormValid,
        isFormDirty,
    } = useCreateRequestForm(editRequests);

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
            {/* <DataTable
                data={formState }
                columns={columns}
                isFetching={isFetchingContract}
                rowIdentifier="id"
            /> */}
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
