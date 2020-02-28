import { DataTableColumn } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import StateComponent from './StateComponent';

const columns: DataTableColumn<PersonnelRequest>[] = [
    {
        accessor: request => request.position?.basePosition?.name || "",
        key: "basePosition",
        label: "Base position",
        sortable: true,
    },

    {
        accessor: request => request.person?.name || "",
        key: "level",
        label: "Level",
        sortable: true,
    },
    {
        accessor: request => request.state,
        key: "state",
        label: "State",
        component: StateComponent,
        sortable: true,
    },
    {
        accessor: request => request.position?.name || "",
        key: "position",
        label: "Position",
        sortable: true,
    },
    {
        accessor: request => request.position?.taskOwner?.positionId || "",
        key: "taskOwnerId",
        label: "Taskowner id",
        sortable: true,
    },

];

export default columns;