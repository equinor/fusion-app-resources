import * as React from 'react';
import * as styles from './styles.less';
import {
    IconButton,
    DeleteIcon,
    EditIcon,
    ErrorMessage,
    AddIcon,
    useTooltipRef,
    CopyIcon,
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
import {
    transformPositionsToChangeRequest,
    transformPositionsToCopyRequest,
} from '../../components/EditRequestSideSheet/utils';
import { useAppContext } from '../../../../../../appContext';

const ActualMppPage: React.FC = () => {
    const [filteredContractPositions, setFilteredContractPositions] = React.useState<Position[]>(
        []
    );
    const [selectedPositions, setSelectedPositions] = React.useState<Position[]>([]);
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);

    const apiClients = useApiClients();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const { apiClient } = useAppContext();
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

    const getPersonnelWithPositionsAsync = async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return;
        }

        const result = await apiClient.getPersonnelWithPositionsAsync(projectId, contractId);
        dispatchContractAction({
            verb: 'merge',
            collection: 'personnel',
            payload: result,
        });
    };

    const fetchPersonnelAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        const result = apiClient.getPersonnelAsync(projectId, contractId);

        getPersonnelWithPositionsAsync();

        return result;
    }, [contract, currentContext]);

    const { data: personnel } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'personnel',
        fetchPersonnelAsync
    );

    const filterSections = React.useMemo(() => {
        return getFilterSections(contractPositions || []);
    }, [contractPositions]);

    const onRequestSidesheetClose = React.useCallback(() => {
        setEditRequests(null);
    }, []);

    const editTooltipRef = useTooltipRef('Create change request for this position');
    const addRequestTooltipRef = useTooltipRef('Create a new request');
    const copyTooltipRef = useTooltipRef('Create new request(s) based on selected positions(s)');

    const editSelected = React.useCallback(
        (copy?: boolean) => {
            const transformedPositions = copy
                ? transformPositionsToCopyRequest(selectedPositions, personnel)
                : transformPositionsToChangeRequest(selectedPositions, personnel);
            setEditRequests(transformedPositions);
        },
        [selectedPositions, personnel]
    );

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
                    <IconButton
                        onClick={() => setEditRequests([])}
                        disabled={selectedPositions.length !== 0}
                        ref={addRequestTooltipRef}
                    >
                        <AddIcon />
                    </IconButton>
                    <IconButton
                        onClick={() => editSelected(true)}
                        disabled={selectedPositions.length <= 0}
                        ref={copyTooltipRef}
                    >
                        <CopyIcon />
                    </IconButton>
                    <IconButton
                        ref={editTooltipRef}
                        onClick={() => editSelected()}
                        disabled={selectedPositions.length === 0}
                    >
                        <EditIcon outline />
                    </IconButton>
                    <IconButton disabled>
                        <DeleteIcon outline />
                    </IconButton>
                </div>
                <SortableTable
                    data={filteredContractPositions || []}
                    columns={columns}
                    rowIdentifier="id"
                    isFetching={isFetching && !contractPositions.length}
                    isSelectable
                    selectedItems={selectedPositions}
                    onSelectionChange={setSelectedPositions}
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
