import * as React from 'react';
import {
    DataTableColumn,
} from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';
import AzureAdStatusIcon from './components/AzureAdStatus';


export type DataItemProps = {
    item: Personnel;
};

const PersonnelColumns = (): DataTableColumn<Personnel>[] => [
    {
        key: 'FirstName',
        accessor: p => p.firstName ? p.firstName : p.name,
        label: 'First Name',
        priority: 1,
        sortable: true,
    },
    {
        key: 'LastName',
        accessor: p => p.lastName ? p.lastName : "",
        label: 'Last Name',
        priority: 2,
        sortable: true,
    },
    {
        key: 'Mail',
        accessor: 'mail',
        label: 'E-Mail',
        priority: 5,
        sortable: true,
    },
    {
        key: 'Phone',
        accessor: 'phoneNumber',
        label: 'Phone Number',
        priority: 10,
        sortable: true,
    },
    {
        key: 'azureAdStatus',
        accessor: 'azureAdStatus',
        label: 'AD',
        priority: 15,
        component: (p) => AzureAdStatusIcon(p.item.azureAdStatus),
        sortable: true,
        width: '20px',
    },

    {
        key: 'Workload',
        accessor: r => '???',
        label: 'Workload',
        priority: 20,
        sortable: true,
        width: '20px',
    },
    {
        key: 'positions',
        accessor: r => '???',
        label: 'Positions',
        priority: 25,
        sortable: true,
        width: '20px',
    },
];

export default PersonnelColumns;
