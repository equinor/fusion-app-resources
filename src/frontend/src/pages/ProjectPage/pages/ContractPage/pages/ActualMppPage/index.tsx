import styles from './styles.less';
import {
    IconButton,
    DeleteIcon,
    EditIcon,
    AddIcon,
    useTooltipRef,
    CopyIcon,
    Spinner,
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
import {
    transformPositionsToChangeRequest,
    transformPositionsToCopyRequest,
} from '../../components/EditRequestSideSheet/utils';
import { useAppContext } from '../../../../../../appContext';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import usePositionDeletion from '../../hooks/usePositionDeletion';
import { FC, useState, useCallback, useMemo } from 'react';
import PositionWithPersonnel from '../../../../../../models/PositionWithPersonnel';

const ActualMppPage: FC = () => {
    const [filteredContractPositions, setFilteredContractPositions] = useState<
        PositionWithPersonnel[]
    >([]);
    const [selectedPositions, setSelectedPositions] = useState<Position[]>([]);
    const [editRequests, setEditRequests] = useState<PersonnelRequest[] | null>(null);

    const apiClients = useApiClients();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();

    const fetchMppAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.externalId;
        if (!contractId || !projectId) {
            return [];
        }

        const response = await apiClients.org.getContractPositionsAsync(projectId, contractId);
        return response.data;
    }, [contract, currentContext]);

    const {
        data: contractPositions,
        isFetching,
        error,
    } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'actualMpp',
        fetchMppAsync,
        'set'
    );

    const { deletePositions, isDeleting, canDeletePosition } =
        usePositionDeletion(selectedPositions);

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

    const fetchPersonnelAsync = useCallback(async () => {
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

    const tableData: PositionWithPersonnel[] = useMemo(
        () =>
            contractPositions.map((position) => ({
                ...position,
                instances: position.instances.map((instance) => ({
                    ...instance,
                    personnelDetails: personnel.find(
                        (person) =>
                            person.azureUniquePersonId === instance.assignedPerson?.azureUniqueId
                    ),
                })),
            })),
        [personnel, contractPositions]
    );

    const filterSections = useMemo(() => {
        return getFilterSections(tableData || []);
    }, [tableData]);

    const onRequestSidesheetClose = useCallback(() => {
        setEditRequests(null);
    }, []);

    const editTooltipRef = useTooltipRef('Create change request for this position');
    const addRequestTooltipRef = useTooltipRef('Create a new request');
    const copyTooltipRef = useTooltipRef('Create new request(s) based on selected positions(s)');

    const editSelected = useCallback(
        (copy?: boolean) => {
            const transformedPositions = copy
                ? transformPositionsToCopyRequest(selectedPositions, personnel)
                : transformPositionsToChangeRequest(selectedPositions, personnel);
            setEditRequests(transformedPositions);
        },
        [selectedPositions, personnel]
    );

    return (
        <div className={styles.actualMppContainer}>
            <ResourceErrorMessage error={error}>
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
                        <IconButton
                            onClick={deletePositions}
                            disabled={selectedPositions.length === 0 || !canDeletePosition}
                        >
                            {isDeleting ? <Spinner inline /> : <DeleteIcon outline />}
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
                    data={tableData}
                    filterSections={filterSections}
                    onFilter={(filteredRequests) => setFilteredContractPositions(filteredRequests)}
                />
                <EditRequestSideSheet
                    initialRequests={editRequests}
                    onClose={onRequestSidesheetClose}
                />
                <PositionDetailsSideSheet positions={contractPositions} />
            </ResourceErrorMessage>
        </div>
    );
};

export default ActualMppPage;
