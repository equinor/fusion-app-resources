import * as React from 'react'
import { DataTable, DataTableColumn, Button } from '@equinor/fusion-components';
import { useSorting, useCurrentContext } from '@equinor/fusion';
import PersonnelColumns from './PersonnelColumns';
import usePersonnel from './hooks/usePersonnel';
import Personnel from '../../../../../../models/Personnel';
import * as styles from './styles.less'
import { useContractContext } from '../../../../../../contractContex';
import AddPersonnelSideSheet from './AddPersonnelSideSheet'
import { v1 as uuid } from 'uuid';


const ManagePersonnelPage: React.FC = () => {
    const currentContext = useCurrentContext()
    const currentContract = useContractContext()
    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel(currentContract?.contract?.id || undefined, currentContext?.id);
    const { sortedData, setSortBy, sortBy, direction } = useSorting<Personnel>(personnel, "name", "asc");
    const [isAddPersonOpen, setIsAddPersonOpen] = React.useState<boolean>(false);
    const [selectedItems, setSelectedItems] = React.useState<Personnel[]>([]);

    const onSortChange = React.useCallback(
        (column: DataTableColumn<Personnel>) => {
            setSortBy(column.accessor, null);
        },
        [sortBy, direction]
    );

    const personnelColumns = React.useMemo(() => PersonnelColumns(), []);
    const sortedByColumn = React.useMemo(() => personnelColumns.find(c => c.accessor === sortBy) || null, [sortBy]);

    return (
        <div className={styles.container}>
            <div className={styles.button}><Button outlined onClick={() => setIsAddPersonOpen(true)} > + Add Person </Button></div>
            <DataTable
                columns={personnelColumns}
                data={sortedData}
                isFetching={isFetchingPersonnel}
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
            {isAddPersonOpen && <AddPersonnelSideSheet isOpen={isAddPersonOpen} setIsOpen={setIsAddPersonOpen} selectedPersonnel={selectedItems.length ? selectedItems : null} />}
        </div>
    );
}

export default ManagePersonnelPage