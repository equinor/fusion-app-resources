import * as React from 'react';
import * as styles from './styles.less';
import {
    Button,
    IconButton,
    DeleteIcon,
    EditIcon,
    ErrorMessage,
} from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../appContext';
import SortableTable from '../components/SortableTable';
import columns from './columns';

const ActiveRequestsPage: React.FC = () => {
    const [activeRequests, setActiveRequests] = React.useState<PersonnelRequest[]>([]);
    const [isFetching, setIsFetching] = React.useState<boolean>(false);
    const [error, setError] = React.useState(null);
    const [selectedRequests, setSelectedRequests] = React.useState<PersonnelRequest[]>([]);
    const { apiClient } = useAppContext();
    const getRequestsAsync = async () => {
        setIsFetching(true);
        setError(null);
        try {
            const response = await apiClient.getPersonnelRequestsAsync('123', '123', true); //TESTING VALUES
            setActiveRequests(response.value);
        } catch (e) {
            setError(e);
        } finally {
            setIsFetching(false);
        }
    };

    React.useEffect(() => {
        getRequestsAsync();
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
