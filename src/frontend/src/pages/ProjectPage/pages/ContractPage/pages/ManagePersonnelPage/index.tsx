import * as React from 'react';
import { DataTable, DataTableColumn } from '@equinor/fusion-components';
import { useSorting, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import PersonnelColumns from './PersonnelColumns';
import Personnel from '../../../../../../models/Personnel';
import * as styles from './styles.less';
import { useContractContext } from '../../../../../../contractContex';
import AddPersonnelSideSheet from './AddPersonnelSideSheet';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import { useAppContext } from '../../../../../../appContext';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import ManagePersonnelToolBar, { IconButtonProps } from './components/ManagePersonnelToolBar';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';

const ManagePersonnelPage: React.FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const [filteredPersonnel, setFilteredPersonnel] = React.useState<Personnel[]>([]);
    const [isAddPersonOpen, setIsAddPersonOpen] = React.useState<boolean>(false);
    const [selectedItems, setSelectedItems] = React.useState<Personnel[]>([]);
    const notification = useNotificationCenter();

    React.useEffect(() => {
        if (!isAddPersonOpen) {
            setSelectedItems([]);
        }
    }, [isAddPersonOpen]);

    const getPersonnelWithPositionsAsync = async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return;
        }

        const result = await apiClient.getPersonnelWithPositionsAsync(projectId, contractId);
        dispatchContractAction({
            verb: 'set',
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

        const result = await apiClient.getPersonnelAsync(projectId, contractId);
        getPersonnelWithPositionsAsync();
        return result;
    }, [contract, currentContext]);

    const { data: personnel, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'personnel',
        fetchPersonnelAsync,
        'set'
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

    const personnelColumns = React.useMemo(() => PersonnelColumns(contract?.id), [contract]);
    const sortedByColumn = React.useMemo(
        () => personnelColumns.find((c) => c.accessor === sortBy) || null,
        [sortBy]
    );

    const deletePersonnelAsync = React.useCallback(
        async (personnelToDelete: Personnel[]) => {
            const contractId = contract?.id;
            if (!currentContext?.id || !contractId) return;

            try {
                const response = await Promise.all(
                    personnelToDelete.map(
                        async (person) =>
                            await apiClient.deletePersonnelAsync(
                                currentContext.id,
                                contractId,
                                person
                            )
                    )
                );

                notification({
                    level: 'low',
                    title: 'Selected personnel deleted',
                    cancelLabel: 'dismiss',
                });

                dispatchContractAction({
                    verb: 'delete',
                    collection: 'personnel',
                    payload: personnelToDelete,
                });
                setSelectedItems([]);
            } catch (e) {
                console.log('exception', e);
                //TODO: This could probably be more helpfull.
                notification({
                    level: 'high',
                    title:
                        'Something went wrong while saving. Please try again or contact administrator',
                });
            }
        },
        [currentContext?.id, personnel]
    );

    const onDeletePersonnel = React.useCallback(async () => {
        const response = await notification({
            level: 'high',
            title: `Are you sure you want to delete ${selectedItems.length} entries from personnel`,
            confirmLabel: "I'm sure",
            cancelLabel: 'Cancel',
        });
        if (response.confirmed) {
            deletePersonnelAsync(selectedItems);
        }
    }, [selectedItems, deletePersonnelAsync]);

    const addButton = React.useMemo((): IconButtonProps => {
        return {
            onClick: () => {
                setSelectedItems([]);
                setIsAddPersonOpen(true);
            },
            disabled: Boolean(selectedItems.length),
        };
    }, [selectedItems]);

    const editButton = React.useMemo((): IconButtonProps => {
        return { onClick: () => setIsAddPersonOpen(true), disabled: !selectedItems.length };
    }, [selectedItems]);

    const deleteButton = React.useMemo((): IconButtonProps => {
        return { onClick: onDeletePersonnel, disabled: !selectedItems.length };
    }, [selectedItems, onDeletePersonnel]);

    return (
        <div className={styles.container}>
            <ResourceErrorMessage error={error}>
                <div className={styles.managePersonnel}>
                    <div className={styles.toolbar}>
                        <ManagePersonnelToolBar
                            addButton={addButton}
                            editButton={editButton}
                            deleteButton={deleteButton}
                        />
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
            </ResourceErrorMessage>
        </div>
    );
};

export default ManagePersonnelPage;
