
import styles from './styles.less';
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
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import { FC, useState, useCallback, useMemo } from 'react';

const CompletedRequestsPage: FC = () => {
    const [filteredCompletedRequests, setFilteredCompletedRequests] = useState<
        PersonnelRequest[]
    >([]);

    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();

    const fetchRequestsAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return apiClient.getPersonnelRequestsAsync(projectId, contractId, 'completed');
    }, [contract, currentContext]);
    const { data, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'completedRequests',
        fetchRequestsAsync,
        'set'
    );

    const completedRequests = useMemo(
        () =>
            data
                .filter(
                    request =>
                        !(
                            request.state === 'ApprovedByCompany' &&
                            request.provisioningStatus?.state !== 'Provisioned'
                        )
                )
                .sort(
                    (a, b) =>
                        (b.lastActivity ? b.lastActivity.getTime() : b.created.getTime()) -
                        (a.lastActivity ? a.lastActivity.getTime() : a.created.getTime())
                ),
        [data]
    );

    const filterSections = useMemo(() => {
        return getFilterSections(completedRequests || []);
    }, [completedRequests]);

    return (
        <div className={styles.activeRequestsContainer}>
            <ResourceErrorMessage error={error}>
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
            </ResourceErrorMessage>
        </div>
    );
};

export default CompletedRequestsPage;
