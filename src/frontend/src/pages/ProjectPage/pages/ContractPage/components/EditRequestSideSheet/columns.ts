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
        label: 'Assigned person',
        item: 'PersonnelPicker',
    },
    {
        accessor: item => item.taskOwner,
        accessKey: 'taskOwner',
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
        accessor: item => item.appliesFrom,
        accessKey: 'appliesFrom',
        label: 'Applies from (optional)',
        item: 'DatePicker',
    },
    {
        accessor: item => item.appliesTo,
        accessKey: 'appliesTo',
        label: 'Applies to (optional)',
        item: 'DatePicker',
    },
    {
        accessor: item => item.workload,
        accessKey: 'workload',
        label: 'Workload(%)',
        item: 'TextInput',
    },
    {
        accessor: item => item.obs,
        accessKey: 'obs',
        label: 'OBS (optional)',
        item: 'TextInput',
    },
    {
        accessor: item => item.description,
        accessKey: 'description',
        label: 'Request description (optional)',
        item: 'TextArea',
    }
];

export default columns;
