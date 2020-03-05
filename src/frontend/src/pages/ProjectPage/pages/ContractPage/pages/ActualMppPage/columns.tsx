import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import * as React from 'react';
import PositionColumn from '../../../../components/PositionColumn';
import { Position } from '@equinor/fusion';

type AssignedPersonProps = {
    item: Position;
};
const AssignedPersonComponent: React.FC<AssignedPersonProps> = ({ item }) => {
    const person = item.instances.find(i => i.assignedPerson)?.assignedPerson || undefined;
    return <PersonCard person={person} photoSize="medium" inline />;
};

const columns: DataTableColumn<Position>[] = [
    {
        accessor: request =>
            request.instances.find(i => i.assignedPerson?.name)?.assignedPerson?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: AssignedPersonComponent,
    },
    {
        accessor: request => request.name || 'TBN',
        key: 'position',
        label: 'Position',
        sortable: true,
    },
    {
        accessor: request => request.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
    },
    {
        accessor: request => request.basePosition?.discipline || 'TBN',
        key: 'discipline',
        label: 'Discipline',
        sortable: true,
    },

    {
        accessor: request =>
            request.instances.find(i => i.parentPositionId)?.parentPositionId || '',
        key: 'taskOwnerId',
        label: 'Taskowner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId =
                item.instances.find(i => i.parentPositionId)?.parentPositionId || null;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
    {
        accessor: request =>
            request.instances.find(i => i.workload)?.workload.toString() + '%' || '',
        key: 'workload',
        label: 'Workload',
        sortable: true,
    },
];

export default columns;
