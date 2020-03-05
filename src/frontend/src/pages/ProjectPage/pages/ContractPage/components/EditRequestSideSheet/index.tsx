import * as React from 'react';
import { ModalSideSheet, Button, DataTable } from '@equinor/fusion-components';
import { useContractContext } from '../../../../../../contractContex';
import columns from './columns';

const EditRequestSideSheet: React.FC = () => {
    const { editRequests, setEditRequests, isFetchingContract } = useContractContext();
    const showSideSheet = React.useMemo(() => editRequests !== null, [editRequests]);

    const closeSideSheet = React.useCallback(() => {
        setEditRequests(null);
    }, [setEditRequests]);

    
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
            <DataTable
                data={editRequests || []}
                columns={columns}
                isFetching={isFetchingContract}
                rowIdentifier="id"
            />
        </ModalSideSheet>
    );
};

export default EditRequestSideSheet;
