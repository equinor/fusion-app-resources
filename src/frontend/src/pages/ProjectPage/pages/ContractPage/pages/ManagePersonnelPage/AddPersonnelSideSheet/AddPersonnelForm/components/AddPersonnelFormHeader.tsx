import * as React from 'react';
import * as styles from '../../styles.less';
import SelectionCell from '../../../components/SelectionCell';
import { useTooltipRef } from '@equinor/fusion-components';
import PersonnelLine from '../../models/PersonnelLine';

type AddPersonnelFormHeadProps = {
    formState: PersonnelLine[];
    setSelectionState: (state: boolean) => void;
};

export const AddPersonnelFormHead: React.FC<AddPersonnelFormHeadProps> = ({
    formState,
    setSelectionState,
}) => {
    const isAllSelected = React.useMemo(() => !formState.find((p) => !Boolean(p?.selected)), [
        formState,
    ]);

    const isSomeSelected = React.useMemo(
        () => formState.find((p) => p.selected === true) && !isAllSelected,
        [formState, isAllSelected]
    );

    const onSelectAll = React.useCallback(() => {
        setSelectionState(!isAllSelected);
    }, [isAllSelected, setSelectionState]);

    const selectableTooltipRef = useTooltipRef(
        isAllSelected ? 'Unselect all' : 'Select all',
        'above'
    );

    return (
        <thead className={styles.tableBody}>
            <tr className={styles.tableRow}>
                <th className={styles.tableRowHeaderSelectionCell}>
                    <SelectionCell
                        isSelected={isAllSelected}
                        onChange={onSelectAll}
                        indeterminate={isSomeSelected}
                        ref={selectableTooltipRef}
                    />
                </th>
                <th className={styles.tableRowHeaderSelectionCell}></th>
                <th className={styles.headerRowCell}>#</th>
                <th className={styles.headerRowCell}>First Name</th>
                <th className={styles.headerRowCell}>Last Name</th>
                <th className={styles.headerRowCell}>E-Mail</th>
                <th className={styles.headerRowCell}>Phone Number</th>
                <th className={styles.headerRowCell}>Dawinci (optional)</th>
                <th className={styles.headerRowCell}>Disciplines (optional)</th>
            </tr>
        </thead>
    );
};
