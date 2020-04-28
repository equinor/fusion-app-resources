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
import { v1 as uuid } from 'uuid';
import ExcelParseReponse, { ExcelHeader } from '../../../../../../models/ExcelParseResponse';

const ManagePersonnelPage: React.FC = () => {
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const [filteredPersonnel, setFilteredPersonnel] = React.useState<Personnel[]>([]);
    const [isAddPersonOpen, setIsAddPersonOpen] = React.useState<boolean>(false);
    const [selectedItems, setSelectedItems] = React.useState<Personnel[]>([]);
    const notification = useNotificationCenter();

    const [isUploadFileOpen, setIsUploadFileOpen] = React.useState<boolean>(false);
    const [selectedFile, setSelectedFile] = React.useState<File | null>(null);

    React.useEffect(() => {
        if (!isAddPersonOpen) {
            setSelectedItems([]);
        }
    }, [isAddPersonOpen]);

    type HeaderTextVariations = {
        [key in keyof Personnel]?: string[];
    };

    type ColumnIndexes = {
        [key in keyof Personnel]?: number;
    };

    const headerTextVariations: HeaderTextVariations = {
        firstName: ['firstname', 'first name'],
        lastName: ['lastname', 'last name'],
        jobTitle: ['jobtitle', 'job title', 'job'],
        phoneNumber: ['telephone number', 'telephonenumber', 'phonenumer', 'phone number'],
        mail: ['mail', 'email', 'e-mail'],
        dawinciCode: ['dawincicode', 'dawinci'],
        disciplines: ['disicpline', 'disciplines'],
    };

    const formatExcelReponse = (reponse: ExcelParseReponse) => {
        const { headers, data } = reponse;

        const findColumnIndex = (column: keyof Personnel) => {
            const columnVariations = headerTextVariations[column];
            return columnVariations
                ? headers.find((header) => header.title in columnVariations)?.colIndex || -1
                : -1;
        };
        const columnIndexes:ColumnIndexes = {};

        for(let [key as key in Personnel ,value] of Object.entries(headerTextVariations)){
            columnIndexes[key] = findColumnIndex(key)
        }

        for (const a in headerTextVariations){
            
        }



        const newPersonnel: Personnel[] = data.map(({ items }) => {
            return {
                personnelId: uuid(),
                name: '',
                firstName: findColumnText('firstName'),
                lastName: findColumnText('lastName'),
                phoneNumber: findColumnText('phoneNumber'),
                mail: findColumnText('mail'),
                jobTitle: findColumnText('jobTitle'),
                disciplines: [{ name: findColumnText('disciplines') }],
            };
        });
    };

    const getExcelReponseAsync = async (file: File) => {
        const excelReponse = await apiClient.ExcelImportParserAsync(file);

        if (excelReponse) formatExcelReponse(excelReponse);
    };

    React.useEffect(() => {
        if (selectedFile === null) return;

        getExcelReponseAsync(selectedFile);
    }, [selectedFile]);

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

    const onExcelImport = React.useCallback(async () => {
        setIsUploadFileOpen(true);
    }, []);

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

    const excelImportButton = React.useMemo((): IconButtonProps => {
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
                    {isUploadFileOpen && (
                        <div>
                            <input
                                type="file"
                                onChange={(e) =>
                                    setSelectedFile(e.target.files ? e.target.files[0] : null)
                                }
                            ></input>
                        </div>
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
