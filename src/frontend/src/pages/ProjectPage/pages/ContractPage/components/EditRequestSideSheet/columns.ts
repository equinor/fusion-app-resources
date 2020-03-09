import { EditableTaleColumn } from '../EditableTable';
import { EditRequest } from '.';

const columns: EditableTaleColumn<EditRequest>[] = [
    {
        accessor: item => item.positionName,
        accessKey: 'positionName',
        label: 'Position name',
        item: 'TextInput',
    },
    {
        accessor: item => item.basePosition,
        accessKey: 'basePosition',
        label: 'Base Position',
        item: 'BasePositionPicker',
    },
    {
        accessor: item => item.workload,
        accessKey: 'workload',
        label: 'Workload',
        item: 'TextInput',
    },
    {
        accessor: item => item.positionId,
        accessKey: 'positionId',
        label: 'Position',
        item: 'PositionPicker',
    },
    {
        accessor: item => item.person,
        accessKey: 'person',
        label: 'Assigned Person',
        item: 'PersonPicker',
    },
    
];

export default columns;
