import * as React from 'react';
import * as styles from './styles.less';
import { Button, IconButton, DeleteIcon, EditIcon, ErrorMessage } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../appContext';
import SortableTable from '../components/SortableTable';
import columns from './columns';
import { useContractContext } from '../../../../../../contractContex';
import { useCurrentContext } from '@equinor/fusion';

const ActiveRequestsPage: React.FC = () => {
    const [activeRequests, setActiveRequests] = React.useState<PersonnelRequest[]>([]);
    const [isFetching, setIsFetching] = React.useState<boolean>(false);
    const [error, setError] = React.useState(null);
    const [selectedRequests, setSelectedRequests] = React.useState<PersonnelRequest[]>([]);
    const { apiClient } = useAppContext();
    const contractContext = useContractContext();
    const currentContext = useCurrentContext();

    const getRequestsAsync = async (projectId: string, contractId: string) => {
        setIsFetching(true);
        setError(null);
        try {
            const response = await apiClient.getPersonnelRequestsAsync(projectId, contractId, true);
            setActiveRequests(response.value);
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
            getRequestsAsync(projectId, contractId);
        }
    }, [contractContext, currentContext]);

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
                data={activeRequests}
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

export default ActiveRequestsPage;
