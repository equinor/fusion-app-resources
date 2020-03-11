import { DataTableColumn } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import RequestStateFlow from '../../components/RequestStateFlow';
import * as React from 'react';
import PositionColumn from '../../../../components/PositionColumn';

const columns: DataTableColumn<PersonnelRequest>[] = [
    {
        accessor: request => request.person?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
    },
    {
        accessor: request => request.state.toString(),
        key: 'status',
        label: 'Status',
        component: RequestStateFlow,
        sortable: true,
    },
    {
        accessor: request => request.position?.name || 'TBN',
        key: 'position',
        label: 'Position',
        sortable: true,
    },

    {
        accessor: request => request.position?.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
    },
    {
        accessor: request => request.position?.basePosition?.discipline || 'TBN',
        key: 'discipline',
        label: 'Discipline',
        sortable: true,
    },

    {
        accessor: request =>
            request.position?.instances.find(i => i.parentPositionId)?.parentPositionId || '',
        key: 'taskOwnerId',
        label: 'Taskowner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId =
                item.position?.instances.find(i => i.parentPositionId)?.parentPositionId || null;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
];

export default columns;
