import * as React from 'react';
import * as styles from './styles.less';
import { Button, IconButton, DeleteIcon, EditIcon, ErrorMessage } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../appContext';
import SortableTable from '../../../../../../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import EditRequestSideSheet from '../../components/EditRequestSideSheet';
import RequestDetailsSideSheet from '../../components/RequestDetailsSideSheet';

const ActiveRequestsPage: React.FC = () => {
    const [filteredActiveRequests, setFilteredActiveRequests] = React.useState<PersonnelRequest[]>(
        []
    );
    const [selectedRequests, setSelectedRequests] = React.useState<PersonnelRequest[]>([]);
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);
    
    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction,  } = useContractContext();
    const currentContext = useCurrentContext();

    const fetchRequestsAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return apiClient.getPersonnelRequestsAsync(projectId, contractId, true);
    }, [contract, currentContext]);
    const { data: activeRequests, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'activeRequests',
        fetchRequestsAsync
    );
    console.log(activeRequests)
    const filterSections = React.useMemo(() => {
        return getFilterSections(activeRequests || []);
    }, [activeRequests]);

    const editRequest = React.useCallback((requests: PersonnelRequest[]) => {
        setEditRequests(requests);
    }, [setEditRequests]);

    if (error) {
        return (
            <ErrorMessage
                hasError
                message="An error occurred while trying to fetch active requests"
            />
        );
    }

    return (
        <div className={styles.activeRequestsContainer}>
            <div className={styles.activeRequests}>
                <div className={styles.toolbar}>
                    <Button onClick={() => editRequest([])}>Request personnel</Button>
                    <div>
                        <IconButton disabled>
                            <DeleteIcon />
                        </IconButton>
                        <IconButton onClick={() => editRequest(selectedRequests)}>
                            <EditIcon />
                        </IconButton>
                    </div>
                </div>
                <SortableTable
                    data={filteredActiveRequests || []}
                    columns={columns}
                    rowIdentifier="id"
                    isFetching={isFetching && !activeRequests.length}
                    isSelectable
                    selectedItems={selectedRequests}
                    onSelectionChange={setSelectedRequests}
                />
            </div>
            <GenericFilter
                data={activeRequests}
                filterSections={filterSections}
                onFilter={setFilteredActiveRequests}
            />
            <EditRequestSideSheet initialRequests={editRequests}/>
            <RequestDetailsSideSheet requests={activeRequests}/>
        </div>
    );
};

export default ActiveRequestsPage;
