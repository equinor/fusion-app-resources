import { DataTable, DataTableColumn, RadioButton } from '@equinor/fusion-components';
import { useSorting, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import PersonnelColumns from './PersonnelColumns';
import Personnel from '../../../../../../models/Personnel';
import styles from './styles.less';
import { useContractContext } from '../../../../../../contractContex';
import AddPersonnelSideSheet from './AddPersonnelSideSheet';
import getFilterSections from './getFilterSections';
import GenericFilter from '../../../../../../components/GenericFilter';
import { useAppContext } from '../../../../../../appContext';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import ManagePersonnelToolBar, { IconButtonProps } from './components/ManagePersonnelToolBar';
import ResourceErrorMessage from '../../../../../../components/ResourceErrorMessage';
import useExcelImport from '../../../../../../hooks/useExcelImport';
import personnelExcelImportSettings from './personnelExcelImportSettings';
import ExcelImportSideSheet from './components/ExcelImportSideSheet';
import { FC, useState, useEffect, useCallback, useMemo } from 'react';
import RadioFilter from './components/RadioFilter';

type MPPFilter = 'all' | 'show-mpp' | 'hide-mpp';

const ManagePersonnelPage: FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { setSelectedFile, isProccessingFile, processedFile, processingError } =
        useExcelImport<Personnel>(personnelExcelImportSettings);

    const { contract, contractState, dispatchContractAction } = useContractContext();
    const [filteredPersonnel, setFilteredPersonnel] = useState<Personnel[]>([]);
    const [isAddPersonOpen, setIsAddPersonOpen] = useState<boolean>(false);
    const [isExcelImport, setIsExcelImport] = useState<boolean>(false);
    const [selectedItems, setSelectedItems] = useState<Personnel[]>([]);
    const notification = useNotificationCenter();

    const [isUploadFileOpen, setIsUploadFileOpen] = useState<boolean>(false);

    const [selectedMppFilter, setSelectedMppFilter] = useState<MPPFilter>('all');

    useEffect(() => {
        if (!isAddPersonOpen) {
            setIsExcelImport(false);
            setSelectedItems([]);
        }
    }, [isAddPersonOpen]);

    useEffect(() => {
        if (processedFile) {
            setSelectedItems([...processedFile]);
            setIsExcelImport(true);
            setIsUploadFileOpen(false);
            setIsAddPersonOpen(true);
        }
    }, [processedFile]);

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

    const fetchPersonnelAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        const result = await apiClient.getPersonnelAsync(projectId, contractId);
        getPersonnelWithPositionsAsync();
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
    const hiddenInMppPersonnel = useMemo(
        () => personnel.filter((p) => !p.positions || p.positions.length === 0),
        [personnel]
    );
    const shownInMppPersonnel = useMemo(
        () => personnel.filter((p) => p.positions && p.positions.length > 0),
        [personnel]
    );

    const mppFilteredPersonnel = useMemo(() => {
        if (selectedMppFilter == 'hide-mpp') {
            return hiddenInMppPersonnel;
        }
        if (selectedMppFilter == 'show-mpp') {
            return shownInMppPersonnel;
        }
        return personnel;
    }, [personnel, selectedMppFilter, hiddenInMppPersonnel, shownInMppPersonnel]);

    const { sortedData, setSortBy, sortBy, direction } = useSorting<Personnel>(
        filteredPersonnel,
        'name',
        'asc'
    );

    const onSortChange = useCallback(
        (column: DataTableColumn<Personnel>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    const filterSections = useMemo(() => {
        return getFilterSections(personnel);
    }, [personnel]);

    const personnelColumns = useMemo(() => PersonnelColumns(contract?.id), [contract]);
    const sortedByColumn = useMemo(
        () => personnelColumns.find((c) => c.accessor === sortBy) || null,
        [sortBy]
    );

    const deletePersonnelAsync = useCallback(
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
                setIsExcelImport(false);
            } catch (e) {
                notification({
                    level: 'high',
                    title: 'Something went wrong while saving. Please try again or contact administrator',
                });
            }
        },
        [currentContext?.id, personnel]
    );

    const onDeletePersonnel = useCallback(async () => {
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

    const onExcelImport = useCallback(async () => {
        setIsUploadFileOpen(true);
    }, []);

    const addButton = useMemo((): IconButtonProps => {
        return {
            onClick: () => {
                setSelectedItems([]);
                setIsExcelImport(false);
                setIsAddPersonOpen(true);
            },
            disabled: Boolean(selectedItems.length),
        };
    }, [selectedItems]);

    const editButton = useMemo((): IconButtonProps => {
        return { onClick: () => setIsAddPersonOpen(true), disabled: !selectedItems.length };
    }, [selectedItems]);

    const deleteButton = useMemo((): IconButtonProps => {
        return { onClick: onDeletePersonnel, disabled: !selectedItems.length };
    }, [selectedItems, onDeletePersonnel]);

    const excelImportButton = useMemo((): IconButtonProps => {
        return { onClick: onExcelImport };
    }, []);

    return (
        <div className={styles.container}>
            <ResourceErrorMessage error={error}>
                <div className={styles.managePersonnel}>
                    <div className={styles.toolbar}>
                        <ManagePersonnelToolBar
                            addButton={addButton}
                            editButton={editButton}
                            deleteButton={deleteButton}
                            excelImportButton={excelImportButton}
                        />
                        <div className={styles.mppFilter}>
                            <RadioFilter
                                radioKey="all"
                                selectedKey={selectedMppFilter}
                                onClick={setSelectedMppFilter}
                                title={`All (${personnel.length})`}
                            />
                            <RadioFilter
                                radioKey="show-mpp"
                                selectedKey={selectedMppFilter}
                                onClick={setSelectedMppFilter}
                                title={`Show people in MPP (${shownInMppPersonnel.length})`}
                            />
                            <RadioFilter
                                radioKey="hide-mpp"
                                selectedKey={selectedMppFilter}
                                onClick={setSelectedMppFilter}
                                title={`Hide people in MPP (${hiddenInMppPersonnel.length})`}
                            />
                        </div>
                    </div>
                    <div className={styles.table}>
                        <DataTable
                            id="contract-personnel-table"
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
                            onSelectionChange={(item) => {
                                setIsExcelImport(false);
                                setSelectedItems(item);
                            }}
                            selectedItems={selectedItems}
                        />
                    </div>
                    {isAddPersonOpen && (
                        <AddPersonnelSideSheet
                            id="add-personnel-sidesheet"
                            isOpen={isAddPersonOpen}
                            setIsOpen={setIsAddPersonOpen}
                            selectedPersonnel={selectedItems.length ? selectedItems : null}
                            excelImport={isExcelImport}
                        />
                    )}
                </div>
                <GenericFilter
                    data={mppFilteredPersonnel}
                    filterSections={filterSections}
                    onFilter={setFilteredPersonnel}
                />
                <ExcelImportSideSheet
                    id="excel-sidesheet"
                    setSelectedFile={setSelectedFile}
                    isProccessing={isProccessingFile}
                    isOpen={isUploadFileOpen}
                    setIsOpen={setIsUploadFileOpen}
                    processingError={processingError}
                />
            </ResourceErrorMessage>
        </div>
    );
};

export default ManagePersonnelPage;
