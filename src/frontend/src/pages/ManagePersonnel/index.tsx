import * as React from 'react'
import { DataTable, DataTableColumn } from '@equinor/fusion-components';
import { useSorting } from '@equinor/fusion';
import PersonnelColumn from './PersonnelColumn';
import Personnel from '../../models/Personnel';


const ManagePersonnel: React.FC = () => {
    
    const { setSortBy, sortBy, direction } = useSorting<Personnel>([], null, null);
    const onSortChange = React.useCallback(
        (column: DataTableColumn<Personnel>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    const sortedByColumn = PersonnelColumn.find(c => c.accessor === sortBy) || null;


    return (
        <DataTable
            columns={PersonnelColumn}
            data={data}
            isFetching={isFetching}
            rowIdentifier={'AzureUniquePersonId'}
            onSortChange={onSortChange}
            sortedBy={{
                column: sortedByColumn,
                direction,
            }}
        />
    );

}

export default ManagePersonnel