import * as React from 'react';
import * as styles from './styles.less';
import { Button, Spinner } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../appContext';
import SortableTable from '../../../../../../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import {
    useCurrentContext,
    HttpClientRequestFailedError,
    FusionApiHttpErrorResponse,
    useNotificationCenter,
} from '@equinor/fusion';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import RequestDetailsSideSheet from '../../components/RequestDetailsSideSheet';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';

let fetchUpdateInterval: NodeJS.Timeout;
const ProvisioningRequestsPage: React.FC = () => {
    const [filteredProvisioningRequests, setFilteredProvisioningRequests] = React.useState<
        PersonnelRequest[]
    >([]);
    const [checkForRequestStatus, setCheckForRequestStatus] = React.useState<boolean>(false);

    const sendNotification = useNotificationCenter();
    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();

    const fetchRequestsAsync = React.useCallback(async () => {
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
    const provisioningRequests = React.useMemo(
        () =>
            data.filter(
                request =>
                    request.state === 'ApprovedByCompany' &&
                    request.provisioningStatus?.state !== 'Provisioned'
            ),
        [data]
    );

    const getRequestsAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return;
        }
        try {
            const response = provisioningRequests.map(request =>
                apiClient.getPersonnelRequestAsync(projectId, contractId, request.id)
            );
            const updatedRequests = await Promise.all(response);
            dispatchContractAction({
                collection: 'completedRequests',
                verb: 'merge',
                payload: updatedRequests,
            });
            const finishedRequests = updatedRequests.filter(
                r => r.provisioningStatus?.state === 'Provisioned'
            );
            if (finishedRequests.length > 0) {
                sendNotification({
                    level: 'low',
                    title: `${finishedRequests
                        .map(r => r.position?.name || '')
                        .join(', ')} has been completed!`,
                });
            }
        } catch (e) {
            if (error instanceof HttpClientRequestFailedError) {
                const requestError = error as HttpClientRequestFailedError<
                    FusionApiHttpErrorResponse
                >;
                sendNotification({
                    level: 'medium',
                    title: requestError.message,
                });
            }
        }
    }, [provisioningRequests, contract, currentContext, dispatchContractAction]);

    React.useEffect(() => clearTimeout(fetchUpdateInterval), []);
    React.useEffect(() => {
        if (provisioningRequests.length <= 0) {
            clearTimeout(fetchUpdateInterval);
            setCheckForRequestStatus(false);
        }
    }, [provisioningRequests]);

    const periodicallyCheckForUpdate = () => {
        setCheckForRequestStatus(true);

        fetchUpdateInterval = setTimeout(function checkForStatus() {
            getRequestsAsync();
            fetchUpdateInterval = setTimeout(checkForStatus, 5000);
        }, 5000);

        setTimeout(() => {
            clearTimeout(fetchUpdateInterval);
            setCheckForRequestStatus(false);
        }, 120000);
    };

    const filterSections = React.useMemo(() => {
        return getFilterSections(provisioningRequests || []);
    }, [provisioningRequests]);

    return (
        <div className={styles.activeRequestsContainer}>
            <ResourceErrorMessage error={error}>
                <div className={styles.activeRequests}>
                    <div className={styles.activeRequestsButton}>
                        <Button
                            disabled={checkForRequestStatus || provisioningRequests.length <= 0}
                            onClick={() =>
                                !checkForRequestStatus &&
                                provisioningRequests.length > 0 &&
                                periodicallyCheckForUpdate()
                            }
                        >
                            {checkForRequestStatus ? (
                                <Spinner small inline />
                            ) : (
                                'Check for request status'
                            )}
                        </Button>
                    </div>

                    <SortableTable
                        data={filteredProvisioningRequests || []}
                        columns={columns}
                        rowIdentifier="id"
                        isFetching={isFetching && !provisioningRequests.length}
                    />
                </div>
                <GenericFilter
                    data={provisioningRequests}
                    filterSections={filterSections}
                    onFilter={setFilteredProvisioningRequests}
                />

                <RequestDetailsSideSheet requests={provisioningRequests} />
            </ResourceErrorMessage>
        </div>
    );
};

export default ProvisioningRequestsPage;
