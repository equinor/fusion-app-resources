import * as React from 'react';
import * as styles from './styles.less';
import {
    Button,
    IconButton,
    DeleteIcon,
    EditIcon,
    ErrorMessage,
    CloseCircleIcon,
    styling,
    Spinner,
    CheckCircleIcon,
    AddIcon,
    useTooltipRef,
} from '@equinor/fusion-components';
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
import useRequestApproval from '../../hooks/useRequestApproval';
import RejectPersonnelSideSheet from '../../components/RejectRequestSideSheet';
import useRequestRejection from '../../hooks/useRequestRejection';

const ActiveRequestsPage: React.FC = () => {
    const [filteredActiveRequests, setFilteredActiveRequests] = React.useState<PersonnelRequest[]>(
        []
    );
    const [selectedRequests, setSelectedRequests] = React.useState<PersonnelRequest[]>([]);
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);
    const [rejectRequest, setRejectRequest] = React.useState<PersonnelRequest[]>([]);

    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();
    
    const { approve, canApprove, isApproving } = useRequestApproval(selectedRequests);
    const { reject, canReject, isRejecting } = useRequestRejection(selectedRequests);

    const addRequestTooltipRef = useTooltipRef('Create a new request');
    const editRequestTooltipRef = useTooltipRef('Edit selected requests');

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

    React.useEffect(() => {
        setSelectedRequests([]);
    }, [activeRequests]);

    const filterSections = React.useMemo(() => {
        return getFilterSections(activeRequests || []);
    }, [activeRequests]);

    const requestPersonnel = React.useCallback(() => {
        setEditRequests([]);
    }, []);

    const editRequest = React.useCallback(() => {
        setEditRequests(selectedRequests);
    }, [selectedRequests]);

    const onRequestSidesheetClose = React.useCallback(() => {
        setEditRequests(null);
        setSelectedRequests([]);
    }, []);

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
                    <div>
                        <IconButton onClick={requestPersonnel} ref={addRequestTooltipRef}>
                            <AddIcon />
                        </IconButton>
                        <IconButton disabled>
                            <DeleteIcon />
                        </IconButton>
                        <IconButton
                            onClick={editRequest}
                            disabled={selectedRequests.length <= 0}
                            ref={editRequestTooltipRef}
                        >
                            <EditIcon />
                        </IconButton>
                    </div>

                    <div className={styles.buttonContainer}>
                        <Button
                            outlined
                            disabled={!canReject}
                            onClick={() => setRejectRequest(selectedRequests)}
                        >
                            <div className={styles.buttonIcon}>
                                {isRejecting ? (
                                    <Spinner small inline />
                                ) : (
                                    <CloseCircleIcon
                                        width={styling.numericalGrid(2)}
                                        height={styling.numericalGrid(2)}
                                    />
                                )}
                            </div>
                            Reject
                        </Button>
                        <Button disabled={!canApprove} onClick={() => canApprove && approve()}>
                            <div className={styles.buttonIcon}>
                                {isApproving ? (
                                    <Spinner small inline />
                                ) : (
                                    <CheckCircleIcon
                                        width={styling.numericalGrid(2)}
                                        height={styling.numericalGrid(2)}
                                    />
                                )}
                            </div>
                            Approve
                        </Button>
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
            <EditRequestSideSheet
                initialRequests={editRequests}
                onClose={onRequestSidesheetClose}
            />
            <RequestDetailsSideSheet requests={activeRequests} />
            <RejectPersonnelSideSheet
                requests={rejectRequest}
                setRequests={setRejectRequest}
                onReject={reason => reject(reason)}
            />
        </div>
    );
};

export default ActiveRequestsPage;
