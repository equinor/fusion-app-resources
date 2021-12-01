
import styles from './styles.less';
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
import { FC, useState, useCallback, useEffect, useMemo } from 'react';

const ActiveRequestsPage: FC = () => {
    const [filteredActiveRequests, setFilteredActiveRequests] = useState<PersonnelRequest[]>(
        []
    );
    const [selectedRequests, setSelectedRequests] = useState<PersonnelRequest[]>([]);
    const [editRequests, setEditRequests] = useState<PersonnelRequest[] | null>(null);
    const [rejectRequest, setRejectRequest] = useState<PersonnelRequest[]>([]);

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

    const fetchRequestsAsync = useCallback(async () => {
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

    useEffect(() => {
        setSelectedRequests([]);
    }, [activeRequests]);

    const filterSections = useMemo(() => {
        return getFilterSections(activeRequests || []);
    }, [activeRequests]);

    const requestPersonnel = useCallback(() => {
        setEditRequests([]);
    }, []);

    const editRequest = useCallback(
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

    const onRequestSidesheetClose = useCallback(() => {
        setEditRequests(null);
        setSelectedRequests([]);
    }, []);

    return (
        <div className={styles.activeRequestsContainer}>
            <ResourceErrorMessage error={error}>
                <div className={styles.activeRequests}>
                    <div className={styles.toolbar}>
                        <div>
                            <IconButton 
                                id="add-request-btn"
                                onClick={requestPersonnel} 
                                ref={addRequestTooltipRef}>
                                <AddIcon />
                            </IconButton>

                            <IconButton
                                id="add-request-for-selected-position-btn"
                                onClick={() => editRequest(true)}
                                disabled={selectedRequests.length <= 0}
                                ref={copyTooltipRef}
                            >
                                <CopyIcon />
                            </IconButton>
                            <IconButton
                                id="edit-request-btn"
                                onClick={() => editRequest()}
                                disabled={selectedRequests.length <= 0}
                                ref={editRequestTooltipRef}
                            >
                                <EditIcon outline />
                            </IconButton>
                            <IconButton
                                id="remove-request-btn"
                                disabled={selectedRequests.length <= 0}
                                ref={deleteRequestTooltipRef}
                                onClick={deleteRequests}
                            >
                                {isDeleting ? <Spinner inline small /> : <DeleteIcon outline />}
                            </IconButton>
                        </div>

                        <div className={styles.buttonContainer}>
                            <Button
                                id="reject-request-btn"
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
                            <Button id="approve-request-btn" disabled={!canApprove} onClick={() => canApprove && approve()}>
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
                            <div className={styles.helpButton}>
                                <Link data-cy="help-btn" target="_blank" to="/help">
                                    <IconButton ref={helpIconRef}>
                                        <HelpIcon />
                                    </IconButton>
                                </Link>
                            </div>
                        </div>
                    </div>
                    <SortableTable
                        id="active-request-table"
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
