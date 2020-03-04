import * as React from 'react';
import * as styles from './styles.less';
import { Button, IconButton, DeleteIcon, EditIcon, ErrorMessage } from '@equinor/fusion-components';
import { Position, useApiClients, useCurrentContext } from '@equinor/fusion';
import SortableTable from '../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import GenericFilter from '../components/GenericFilter';
import getFilterSections from './getFilterSections';

const ActualMppPage: React.FC = () => {
    const [contractPositions, setContractPositions] = React.useState<Position[] | null>(null);
    const [filteredContractPositions, setFilteredContractPositions] = React.useState<Position[]>(
        []
    );
    const [isFetching, setIsFetching] = React.useState<boolean>(false);
    const [error, setError] = React.useState(null);
    const [selectedRequests, setSelectedRequests] = React.useState<Position[]>([]);
    const apiClients = useApiClients();
    const contractContext = useContractContext();
    const currentContext = useCurrentContext();

    const getContractPositions = async (projectId: string, contractId: string) => {
        setIsFetching(true);
        setError(null);
        try {
            const response = await apiClients.org.getContractPositionsAsync("01302859-f803-42a8-b6fa-4973bce5bc6b", "de1ba6df-6201-4dac-b15a-bb91aa2f34ea");
            setContractPositions(response.data);
        } catch (e) {
            setError(e);
        } finally {
            setIsFetching(false);
        }
    };

    React.useEffect(() => {
        const contractId = contractContext.contract?.id;
        const projectId = currentContext?.id;
        if (contractId && projectId) {
            getContractPositions(projectId, contractId);
        }
    }, [contractContext, currentContext]);

    const filterSections = React.useMemo(() => {
        return getFilterSections(filteredContractPositions);
    }, [filteredContractPositions]);

    if (error) {
        return (
            <ErrorMessage
                hasError
                message="An error occurred while trying to fetch contract personnel data"
            />
        );
    }

    return (
        <div className={styles.actualMppContainer}>
            <div className={styles.actualMpp}>
                <div className={styles.toolbar}>
                    <Button>Request personnel</Button>
                    <div>
                        <IconButton>
                            <DeleteIcon />
                        </IconButton>
                        <IconButton>
                            <EditIcon />
                        </IconButton>
                    </div>
                </div>
                {contractPositions && contractPositions?.length <= 0 ? (
                    <ErrorMessage
                        hasError
                        errorType="noData"
                        message="No positions found on selected contract"
                    />
                ) : (
                    <SortableTable
                        data={filteredContractPositions || []}
                        columns={columns}
                        rowIdentifier="id"
                        isFetching={isFetching}
                        isSelectable
                        selectedItems={selectedRequests}
                        onSelectionChange={setSelectedRequests}
                    />
                )}
            </div>
            <GenericFilter
                data={contractPositions}
                filterSections={filterSections}
                onFilter={filteredRequests => setFilteredContractPositions(filteredRequests)}
            />
        </div>
    );
};

export default ActualMppPage;
