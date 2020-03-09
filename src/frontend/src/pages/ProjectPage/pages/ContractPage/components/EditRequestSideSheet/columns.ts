import { EditableTaleColumn } from '../EditableTable';
import { EditRequest } from '.';

const columns: EditableTaleColumn<EditRequest>[] = [
    {
        accessor: item => item.basePosition,
        accessKey: 'basePosition',
        label: 'Base Position',
        item: 'BasePositionPicker',
    },
    {
        accessor: item => item.person,
        accessKey: 'person',
        label: 'Assigned Person',
        item: 'PersonnelPicker',
    },
    {
        accessor: item => item.parentPosition,
        accessKey: 'parentPosition',
        label: 'Task owner',
        item: 'PositionPicker',
    },
    {
        accessor: item => item.positionName,
        accessKey: 'positionName',
        label: 'Custom position title',
        item: 'TextInput',
    },
    {
        accessor: item => item.workload,
        accessKey: 'workload',
        label: 'Workload',
        item: 'TextInput',
    },
    
];

export default columns;
