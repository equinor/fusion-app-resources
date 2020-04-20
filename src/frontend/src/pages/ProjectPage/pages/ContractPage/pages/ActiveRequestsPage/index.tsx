import * as React from 'react';
import * as styles from './styles.less';
import {
    Button,
    IconButton,
    DeleteIcon,
    EditIcon,
    CloseCircleIcon,
    styling,
    Spinner,
    CheckCircleIcon,
    AddIcon,
    useTooltipRef,
    HelpIcon,
    CopyIcon,
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
import useRequestDeletion from '../../hooks/useRequestDeletion';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import { Link } from 'react-router-dom';

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
    const { deleteRequests, isDeleting } = useRequestDeletion(selectedRequests);

    const addRequestTooltipRef = useTooltipRef('Create a new request');
    const editRequestTooltipRef = useTooltipRef('Edit selected requests');
    const deleteRequestTooltipRef = useTooltipRef('Delete selected request');
    const helpIconRef = useTooltipRef('Help page', 'below');
    const copyTooltipRef = useTooltipRef('Create new request(s) based on selected request(s)');

    const fetchRequestsAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return apiClient.getPersonnelRequestsAsync(projectId, contractId, 'active');
    }, [contract, currentContext]);
    const { data: activeRequests, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'activeRequests',
        fetchRequestsAsync,
        'set'
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

    const editRequest = React.useCallback(
        (copy?: boolean) => {
            const requests: PersonnelRequest[] = copy
                ? selectedRequests.map((s) => ({
                      ...s,
                      id: '',
                      originalPositionId: null,
                  }))
                : selectedRequests;
            setEditRequests(requests);
        },
        [selectedRequests]
    );

    const onRequestSidesheetClose = React.useCallback(() => {
        setEditRequests(null);
        setSelectedRequests([]);
    }, []);

    return (
        <div className={styles.activeRequestsContainer}>
            <ResourceErrorMessage error={error}>
                <div className={styles.activeRequests}>
                    <div className={styles.toolbar}>
                        <div>
                            <IconButton onClick={requestPersonnel} ref={addRequestTooltipRef}>
                                <AddIcon />
                            </IconButton>

                            <IconButton
                                onClick={() => editRequest(true)}
                                disabled={selectedRequests.length <= 0}
                                ref={copyTooltipRef}
                            >
                                <CopyIcon />
                            </IconButton>
                            <IconButton
                                onClick={() => editRequest()}
                                disabled={selectedRequests.length <= 0}
                                ref={editRequestTooltipRef}
                            >
                                <EditIcon outline />
                            </IconButton>
                            <IconButton
                                disabled={selectedRequests.length <= 0}
                                ref={deleteRequestTooltipRef}
                                onClick={deleteRequests}
                            >
                                {isDeleting ? <Spinner inline small /> : <DeleteIcon outline />}
                            </IconButton>
                        </div>

                        <div className={styles.buttonContainer}>
                            {canReject && (
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
                            )}
                            {canApprove && (
                                <Button
                                    disabled={!canApprove}
                                    onClick={() => canApprove && approve()}
                                >
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
                            )}
                            <div className={styles.helpButton}>
                                <Link target="_blank" to="/help">
                                    <IconButton ref={helpIconRef}>
                                        <HelpIcon />
                                    </IconButton>
                                </Link>
                            </div>
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
                    onReject={(reason) => reject(reason)}
                />
            </ResourceErrorMessage>
        </div>
    );
};

export default ActiveRequestsPage;
