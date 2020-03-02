import { DataTableColumn } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import RequestStateFlow from '../components/RequestStateFlow';

const columns: DataTableColumn<PersonnelRequest>[] = [
    {
        accessor: request => request.position?.basePosition?.name || '',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
    },

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
        accessor: request => request.position?.name || '',
        key: 'position',
        label: 'Position',
        sortable: true,
    },
    {
        accessor: request => request.position?.taskOwner?.positionId || '',
        key: 'taskOwnerId',
        label: 'Taskowner id',
        sortable: true,
    },
];

export default columns;
