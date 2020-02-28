import * as React from 'react'
import { DataTable, DataTableColumn, Button } from '@equinor/fusion-components';
import { useSorting, useCurrentContext } from '@equinor/fusion';
import PersonnelColumns from './PersonnelColumns';
import usePersonnel from './hooks/usePersonnel';
import Personnel from '../../../../../../models/Personnel';
import { RouteComponentProps } from 'react-router-dom';
import * as styles from './styles.less'
import { useContractContext } from '../../../../../../contractContex';


type ManagePersonnelPageMatch = {
    contractId: string;
};

type ManagePersonnelProps = RouteComponentProps<ManagePersonnelPageMatch>;


const ManagePersonnelPage: React.FC<ManagePersonnelProps> = () => {
    const currentContext = useCurrentContext()
    const currentContract = useContractContext()
    console.log("contexts:",currentContract?.contract.id,currentContext?.id)
    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel(currentContract?.contract.id,currentContext?.id);
        
    const { setSortBy, sortBy, direction } = useSorting<Personnel>([], null, null);
    const onSortChange = React.useCallback(
        (column: DataTableColumn<Personnel>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    const personnelColumns = React.useMemo(() => PersonnelColumns(), []);
    const sortedByColumn = personnelColumns.find(c => c.accessor === sortBy) || null;

    return (
        <div className= {styles.container}>
            <Button outlined> + Add Person </Button>
            <DataTable 
                columns={personnelColumns}
                data={personnel}
                isFetching={isFetchingPersonnel}
                rowIdentifier={'AzureUniquePersonId'}
                onSortChange={onSortChange}
                sortedBy={{
                    column: sortedByColumn,
                    direction,
                }}
            />
        </div>
    );
}

export default ManagePersonnelPage