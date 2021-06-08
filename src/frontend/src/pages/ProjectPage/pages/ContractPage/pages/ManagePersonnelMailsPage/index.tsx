import { useCurrentContext } from '@equinor/fusion';
import { FC, useCallback } from 'react';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import * as styles from './styles.less';
import PersonnelMailsTable from './PersonnelMailsTable';
import ToolbarFilter from './ToolbarFilter';
import ManagePersonnelMailContext from './ManagePersonnelMailContext';
import usePersonnelContactMail from './usePersonnelContactMail';
import SaveContactMails from './SaveContactMails';

const ManagePersonnelMailsPage: FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();

    const { contract, contractState, dispatchContractAction } = useContractContext();

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
    const {
        contactMailForm,
        filteredPersonnel,
        isContactMailFormDirty,
        updateContactMail,
        setFilteredPersonnel,
        isSavingContactMails,
        saveContactMailsAsync,
        showInputErrors
    } = usePersonnelContactMail(personnel);

    return (
        <ManagePersonnelMailContext
            value={{
                contactMailForm,
                filteredPersonnel,
                isContactMailFormDirty,
                updateContactMail,
                setFilteredPersonnel,
                isSavingContactMails,
                saveContactMailsAsync,
                showInputErrors
            }}
        >
            <div className={styles.container}>
                <ResourceErrorMessage error={error}>
                    <div className={styles.toolBar}>
                        <ToolbarFilter
                            personnel={personnel}
                            setFilteredPersonnel={setFilteredPersonnel}
                            filteredPersonnel={filteredPersonnel}
                        />
                        <SaveContactMails />
                    </div>

                    <div className={styles.managePersonnel}>
                        <PersonnelMailsTable
                            isFetching={isFetching}
                            personnel={filteredPersonnel}
                        />
                    </div>
                </ResourceErrorMessage>
            </div>
        </ManagePersonnelMailContext>
    );
};
export default ManagePersonnelMailsPage;
