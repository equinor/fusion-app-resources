import * as React from 'react';
import * as styles from './styles.less';
import { Button, IconButton, DeleteIcon, EditIcon, ErrorMessage } from '@equinor/fusion-components';
import { Position, useApiClients, useCurrentContext } from '@equinor/fusion';
import SortableTable from '../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';

const ActualMppPage: React.FC = () => {
    const [contractPositions, setContractPositions] = React.useState<Position[]>([])
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
            const response = await apiClients.org.getContractPositionsAsync(projectId, contractId);
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
            getContractPositions(projectId, contractId)
        }
    }, [contractContext, currentContext]);

    if (error) {
        return <ErrorMessage hasError message="An error occurred while trying to fetch contract personnel data" />
    };

    return (
        <div className={styles.actualMppContainer}>
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
            <SortableTable
                data={contractPositions}
                columns={columns}
                rowIdentifier="id"
                isFetching={isFetching}
                isSelectable
                selectedItems={selectedRequests}
                onSelectionChange={setSelectedRequests}
            />
        </div>
    );
};

export default ActualMppPage;
