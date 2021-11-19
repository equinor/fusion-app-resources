import { EditableTaleColumn } from '../EditableTable';
import { EditRequest } from '.';

const columns: EditableTaleColumn<EditRequest>[] = [
    {
        id: 'base-position-input',
        accessor: item => item.basePosition,
        accessKey: 'basePosition',
        label: 'Base Position',
        item: 'BasePositionPicker',
    },
    {
        id: 'assigned-person-input',
        accessor: item => item.person,
        accessKey: 'person',
        label: 'Assigned person',
        item: 'PersonnelPicker',
    },
    {
        id: 'task-owner-input',
        accessor: item => item.taskOwner,
        accessKey: 'taskOwner',
        label: 'Task owner (optional)',
        item: 'PositionPicker',
    },
    {
        id: 'custom-position-input',
        accessor: item => item.positionName,
        accessKey: 'positionName',
        label: 'Custom position title',
        item: 'TextInput',
    },
    {
        id: 'applies-from-input',
        accessor: item => item.appliesFrom,
        accessKey: 'appliesFrom',
        label: 'Applies from',
        item: 'DatePicker',
    },
    {
        id: 'applies-to-input',
        accessor: item => item.appliesTo,
        accessKey: 'appliesTo',
        label: 'Applies to',
        item: 'DatePicker',
    },
    {
        id: 'work-load-input',
        accessor: item => item.workload,
        accessKey: 'workload',
        label: 'Workload (%)',
        item: 'TextInput',
    },
    {
        id: 'obs-input',
        accessor: item => item.obs,
        accessKey: 'obs',
        label: 'OBS (optional)',
        item: 'TextInput',
    },
    {
        id: 'request-description-input',
        accessor: item => item.description,
        accessKey: 'description',
        label: 'Request description (optional)',
        item: 'TextArea',
    }
];

export default columns;
