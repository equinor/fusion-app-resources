import * as React from 'react';
import { DataTable, DataTableColumn, Button, AddIcon } from '@equinor/fusion-components';
import { useSorting, useCurrentContext } from '@equinor/fusion';
import PersonnelColumns from './PersonnelColumns';
import Personnel from '../../../../../../models/Personnel';
import * as styles from './styles.less';
import { useContractContext } from '../../../../../../contractContex';
import AddPersonnelSideSheet from './AddPersonnelSideSheet';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import { useAppContext } from '../../../../../../appContext';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';

const ManagePersonnelPage: React.FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const [filteredPersonnel, setFilteredPersonnel] = React.useState<Personnel[]>([]);
    const [isAddPersonOpen, setIsAddPersonOpen] = React.useState<boolean>(false);
    const [selectedItems, setSelectedItems] = React.useState<Personnel[]>([]);

    const fetchPersonnelAsync = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return apiClient.getPersonnelAsync(projectId, contractId);
    }, [contract, currentContext]);

    const { data: personnel, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'personnel',
        fetchPersonnelAsync
    );


    const { sortedData, setSortBy, sortBy, direction } = useSorting<Personnel>(
        filteredPersonnel,
        'name',
        'asc'
    );

    const onSortChange = React.useCallback(
        (column: DataTableColumn<Personnel>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    const filterSections = React.useMemo(() => {
        return getFilterSections(personnel);
    }, [personnel]);

    const personnelColumns = React.useMemo(() => PersonnelColumns(), []);
    const sortedByColumn = React.useMemo(
        () => personnelColumns.find(c => c.accessor === sortBy) || null,
        [sortBy]
    );

    return (
        <div className={styles.container}>
            <div className={styles.managePersonnel}>
                <div className={styles.toolbar}>
                    <Button outlined onClick={() => setIsAddPersonOpen(true)}>
                        <AddIcon /> Add Person
                    </Button>
                </div>
                <div className={styles.table}>
                    <DataTable
                        columns={personnelColumns}
                        data={sortedData}
                        isFetching={isFetching}
                        rowIdentifier={'personnelId'}
                        onSortChange={onSortChange}
                        sortedBy={{
                            column: sortedByColumn,
                            direction,
                        }}
                        isSelectable
                        onSelectionChange={setSelectedItems}
                        selectedItems={selectedItems}
                    />
                </div>
                >
                {isAddPersonOpen && (
                    <AddPersonnelSideSheet
                        isOpen={isAddPersonOpen}
                        setIsOpen={setIsAddPersonOpen}
                        selectedPersonnel={selectedItems.length ? selectedItems : null}
                    />
                )}
            </div>
            <GenericFilter
                data={personnel}
                filterSections={filterSections}
                onFilter={setFilteredPersonnel}
            />
        </div>
    );
};

export default ManagePersonnelPage;
