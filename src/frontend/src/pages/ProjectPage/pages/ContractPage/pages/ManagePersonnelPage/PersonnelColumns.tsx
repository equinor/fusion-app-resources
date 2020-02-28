import * as React from 'react';
import { DataTableColumn } from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';

export type DataItemProps = {
  item: Personnel;
  rowIndex: number;
};

const AzureAdStatus : React.FC<DataItemProps> = ({ item }) => (
  <div>{item.AzureAdStatus}</div>
)


const PersonnelColumns = ():  DataTableColumn<Personnel>[] => [
  {
    key: 'Name',
    accessor: 'Name',
    label: 'Person',
    priority: 1,
    sortable: true,
  },
  {
    key: 'Mail',
    accessor: 'Mail',
    label: 'E-Mail',
    priority: 5,
    sortable: true,
  },
  {
    key: 'AzureAdStatus',
    accessor: 'AzureAdStatus',
    label: 'AD',
    priority: 10,
    component : AzureAdStatus,
    sortable: true,
  },
  {
    key: 'Phone',
    accessor: 'PhoneNumber',
    label: 'Phone Number',
    priority: 15,
    sortable: true,
  },
  {
    key: 'Disciplines',
    accessor: 'Disciplines',
    label: 'Disciplines',
    priority: 20,
    sortable: true,
  },
]

export default PersonnelColumns
