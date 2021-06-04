import { useCurrentContext } from '@equinor/fusion';
import { FC, useCallback, useEffect, useState } from 'react';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';
import Personnel from '../../../../../../models/Personnel';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import * as styles from './styles.less';
import PersonnelMailsTable from './PersonnelMailsTable';
import ToolbarFilter from './ToolbarFilter';

const ManagePersonnelMailsPage: FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();

    const { contract, contractState, dispatchContractAction } = useContractContext();
    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);

    const fetchPersonnelAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        const result = await apiClient.getPersonnelAsync(projectId, contractId);
        return result;
    }, [contract, currentContext]);

    const {
        data: personnel,
        isFetching,
        error,
    } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'personnel',
        fetchPersonnelAsync,
        'set'
    );

    useEffect(() => {
        setFilteredPersonnel(personnel);
    }, [personnel]);

    return (
        <div className={styles.container}>
            <ResourceErrorMessage error={error}>
                <ToolbarFilter
                    personnel={personnel}
                    setFilteredPersonnel={setFilteredPersonnel}
                    filteredPersonnel={filteredPersonnel}
                />
                <div className={styles.managePersonnel}>
                    <PersonnelMailsTable isFetching={isFetching} personnel={filteredPersonnel} />
                </div>
            </ResourceErrorMessage>
        </div>
    );
};
export default ManagePersonnelMailsPage;
