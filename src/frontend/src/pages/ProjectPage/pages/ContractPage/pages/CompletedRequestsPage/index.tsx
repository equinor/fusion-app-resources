import * as React from 'react';
import * as styles from './styles.less';
import { ErrorMessage } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../appContext';
import SortableTable from '../../../../../../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import RequestDetailsSideSheet from '../../components/RequestDetailsSideSheet';

const CompletedRequestsPage: React.FC = () => {
    const [filteredCompletedRequests, setFilteredCompletedRequests] = React.useState<PersonnelRequest[]>(
        []
    );

    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();

    const fetchRequestsAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return apiClient.getPersonnelRequestsAsync(projectId, contractId, "completed");
    }, [contract, currentContext]);
    const { data: completedRequests, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'completedRequests',
        fetchRequestsAsync
    );

    const filterSections = React.useMemo(() => {
        return getFilterSections(completedRequests || []);
    }, [completedRequests]);

    if (error) {
        return (
            <ErrorMessage
                hasError
                message="An error occurred while trying to fetch completed requests"
            />
        );
    }

    return (
        <div className={styles.activeRequestsContainer}>
            <div className={styles.activeRequests}>
                <SortableTable
                    data={filteredCompletedRequests || []}
                    columns={columns}
                    rowIdentifier="id"
                    isFetching={isFetching && !completedRequests.length}
                />
            </div>
            <GenericFilter
                data={completedRequests}
                filterSections={filterSections}
                onFilter={setFilteredCompletedRequests}
            />

            <RequestDetailsSideSheet requests={completedRequests} />
        </div>
    );
};

export default CompletedRequestsPage;
