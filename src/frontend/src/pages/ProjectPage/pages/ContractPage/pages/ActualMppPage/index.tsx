import * as React from 'react';
import * as styles from './styles.less';
import {
    Button,
    IconButton,
    DeleteIcon,
    EditIcon,
    ErrorMessage,
    AddIcon,
} from '@equinor/fusion-components';
import { Position, useApiClients, useCurrentContext } from '@equinor/fusion';
import SortableTable from '../../../../../../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import GenericFilter from '../../../../../../components/GenericFilter';
import getFilterSections from './getFilterSections';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import EditRequestSideSheet from '../../components/EditRequestSideSheet';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import PositionDetailsSideSheet from '../../components/PositionDetailsSideSheet';
import { ErrorMessageProps } from '@equinor/fusion-components/dist/components/general/ErrorMessage';

const ActualMppPage: React.FC = () => {
    const [filteredContractPositions, setFilteredContractPositions] = React.useState<Position[]>(
        []
    );
    const [selectedRequests, setSelectedRequests] = React.useState<Position[]>([]);
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);

    const apiClients = useApiClients();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();

    const fetchMppAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.externalId;
        if (!contractId || !projectId) {
            return [];
        }

        const response = await apiClients.org.getContractPositionsAsync(projectId, contractId);
        return response.data;
    }, [contract, currentContext]);

    const { data: contractPositions, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'actualMpp',
        fetchMppAsync
    );

    const filterSections = React.useMemo(() => {
        return getFilterSections(contractPositions || []);
    }, [contractPositions]);

    const onRequestSidesheetClose = React.useCallback(() => {
        setEditRequests(null);
    }, []);

    if (error) {
        const errorMessage: ErrorMessageProps = {
            hasError: true,
        };

        switch (error.statusCode) {
            case 403:
                errorMessage.errorType = 'accessDenied';
                errorMessage.message = error.response.error.message;
                errorMessage.resourceName = 'Actual Mpp';
                break;
            default:
                errorMessage.errorType = 'error';
                break;
        }

        return <ErrorMessage {...errorMessage} />;
    }

    return (
        <div className={styles.actualMppContainer}>
            <div className={styles.actualMpp}>
                <div className={styles.toolbar}>
                    <IconButton onClick={() => setEditRequests([])}>
                        <AddIcon />
                    </IconButton>
                    <IconButton disabled>
                        <DeleteIcon />
                    </IconButton>
                    <IconButton disabled>
                        <EditIcon />
                    </IconButton>
                </div>
                <SortableTable
                    data={filteredContractPositions || []}
                    columns={columns}
                    rowIdentifier="id"
                    isFetching={isFetching && !contractPositions.length}
                    isSelectable
                    selectedItems={selectedRequests}
                    onSelectionChange={setSelectedRequests}
                />
            </div>
            <GenericFilter
                data={contractPositions}
                filterSections={filterSections}
                onFilter={filteredRequests => setFilteredContractPositions(filteredRequests)}
            />
            <EditRequestSideSheet
                initialRequests={editRequests}
                onClose={onRequestSidesheetClose}
            />
            <PositionDetailsSideSheet positions={contractPositions} />
        </div>
    );
};

export default ActualMppPage;
