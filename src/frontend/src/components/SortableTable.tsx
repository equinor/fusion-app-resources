import * as React from 'react';
import { DataTableColumn, DataTable } from '@equinor/fusion-components';
import { useSorting, usePagination, Page } from '@equinor/fusion';
import { DataItemComponentProps } from '@equinor/fusion-components/dist/components/data/DataTable/dataTableTypes';
import { useContractContext } from '../contractContex';

type SortableTableProps<T> = {
    data: T[];
    columns: DataTableColumn<T>[];
    rowIdentifier: keyof T | ((item: T) => string);
    expandedComponent?: React.FC<DataItemComponentProps<T>>;
    isSelectable?: boolean;
    selectedItems?: T[];
    onSelectionChange?: (selectedItems: T[]) => void;
    isFetching?: boolean;
};

function SortableTable<T>({
    data,
    columns,
    rowIdentifier,
    expandedComponent,
    isFetching,
    isSelectable,
    onSelectionChange,
    selectedItems,
}: SortableTableProps<T>) {
    const { isFetchingContract } = useContractContext();
    const { sortedData, setSortBy, sortBy, direction } = useSorting<T>(data, null, null);

    const { pagination, pagedData, setCurrentPage } = usePagination<T>(sortedData, 15);

    const onSortChange = React.useCallback(
        (column: DataTableColumn<T>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    React.useEffect(() => {
        if (pagination.pageCount > 0 && pagination.currentPage.index > pagination.pageCount - 1) {
            setCurrentPage(pagination.pageCount - 1, pagination.perPage);
        }
    }, [pagination]);

    const onPaginationChange = React.useCallback((newPage: Page, perPage: number) => {
        setCurrentPage(newPage.index, perPage);
    }, []);

    const sortedByColumn = columns.find(c => c.accessor === sortBy) || null;

    return (
        <DataTable
            columns={columns}
            data={pagedData}
            pagination={pagination}
            expandedComponent={expandedComponent}
            onPaginationChange={onPaginationChange}
            isFetching={!!isFetching || isFetchingContract}
            onSortChange={onSortChange}
            rowIdentifier={rowIdentifier}
            sortedBy={{
                column: sortedByColumn,
                direction,
            }}
            isSelectable={isSelectable}
            onSelectionChange={onSelectionChange}
            selectedItems={selectedItems}
        />
    );
}

export default SortableTable;
