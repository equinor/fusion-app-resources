import * as React from "react";
import { DataTableColumn, DataTable } from "@equinor/fusion-components";
import { useSorting, usePagination, Page } from "@equinor/fusion";
import { DataItemComponentProps } from '@equinor/fusion-components/dist/components/data/DataTable/dataTableTypes';

type SortableTableProps<T> = {
    data: T[],
    columns: DataTableColumn<T>[],
    rowIdentifier: keyof T | ((item: T) => string),
    expandedComponent?: React.FC<DataItemComponentProps<T>>,
    isFetching?: boolean
};

function SortableTable<T>({ data, columns, rowIdentifier, expandedComponent, isFetching }: SortableTableProps<T>) {
    const { sortedData, setSortBy, sortBy, direction } = useSorting<T>(data, null, null);

    const { pagination, pagedData, setCurrentPage } = usePagination<T>(sortedData, 20);

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
            isFetching={!!isFetching}
            onSortChange={onSortChange}
            rowIdentifier={rowIdentifier}
            sortedBy={{
                column: sortedByColumn,
                direction,
            }}
        />

    );
}

export default SortableTable;
