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
        key: 'Mail',
        accessor: 'mail',
        label: 'E-Mail',
        priority: 1,
        sortable: true,
    },
    {
        key: 'FirstName',
        accessor: p => p.firstName || "",
        label: 'First Name',
        priority: 5,
        sortable: true,
    },
    {
        key: 'LastName',
        accessor: p => p.lastName || "",
        label: 'Last Name',
        priority: 6,
        sortable: true,
    },
    {
        key: 'Disciplines',
        accessor: p => p.disciplines.map((d) => d.name)?.join('/') || "",
        label: 'Discipline',
        priority: 10,
        sortable: true,
    },
    {
        key: 'Phone',
        accessor: 'phoneNumber',
        label: 'Phone Number',
        priority: 15,
        sortable: true,
    },
    {
        key: 'azureAdStatus',
        accessor: 'azureAdStatus',
        label: 'AD',
        priority: 20,
        component: (p) => AzureAdStatusIcon(p.item?.azureAdStatus || "NoAccount"),
        sortable: true,
        width: '20px',
    },

    {
        key: 'Workload',
        accessor: r => '-',
        label: 'Workload',
        priority: 25,
        sortable: true,
        width: '20px',
    },
    {
        key: 'positions',
        accessor: r => "0",
        label: 'Nr Positions',
        priority: 30,
        sortable: true,
        width: '20px',
    },
];

export default PersonnelColumns;
