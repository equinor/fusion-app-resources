import { DataTableColumn, PersonCard } from '@equinor/fusion-components';

import PositionColumn from '../../../../components/PositionColumn';
import { Position, useHistory } from '@equinor/fusion';
import styles from './styles.less';
import { FC, useCallback } from 'react';

type AssignedPersonProps = {
    item: Position;
};
const AssignedPersonComponent: FC<AssignedPersonProps> = ({ item }) => {
    const person = item.instances.find(i => i.assignedPerson)?.assignedPerson || undefined;
    return <PersonCard person={person} photoSize="medium" inline />;
};

type ColumnSideSheetLinkProps = {
    positionId: string;
};

const ColumnSideSheetLink: FC<ColumnSideSheetLinkProps> = ({ positionId, children }) => {
    const history = useHistory();

    const openSideSheet = useCallback(() => {
        const sideSheetSearchString = `positionId=${positionId}`;
        history.push({
            pathname: history.location.pathname,
            search: sideSheetSearchString,
        });
    }, [history.location.pathname, positionId]);

    return (
        <div onClick={openSideSheet} className={styles.columnLink}>
            {children}
        </div>
    );
};

const columns: DataTableColumn<Position>[] = [
    {
        accessor: position => position.name || 'TBN',
        key: 'position',
        label: 'Position',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>{item.name || 'TBN'}</ColumnSideSheetLink>
        ),
    },
    {
        accessor: position =>
            position.instances.find(i => i.assignedPerson?.name)?.assignedPerson?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: AssignedPersonComponent,
    },
    {
        accessor: position => position.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                {item.basePosition?.name || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: position => position.basePosition?.discipline || 'TBN',
        key: 'discipline',
        label: 'Discipline',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                {item.basePosition?.discipline || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },

    {
        accessor: position =>
            position.instances.find(i => i.parentPositionId)?.parentPositionId || '',
        key: 'taskOwnerId',
        label: 'Task owner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId =
                item.instances.find(i => i.parentPositionId)?.parentPositionId || null;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
    {
        accessor: position =>
            position.instances.find(i => !isNaN(i.workload))?.workload.toString() + '%' || '',
        key: 'workload',
        label: 'Workload',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                {item.instances.find(i => !isNaN(i.workload))?.workload.toString() + '%' || ''}
            </ColumnSideSheetLink>
        ),
    },
];

export default columns;
