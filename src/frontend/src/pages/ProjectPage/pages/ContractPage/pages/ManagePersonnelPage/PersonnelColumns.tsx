import * as React from 'react';
import { DataTableColumn } from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';

export type DataItemProps = {
  item: Personnel;
  rowIndex: number;
};

const AzureAdStatus : React.FC<DataItemProps> = ({ item }) => (
  //TODO: Add icon instead of text, depending on status
  <div>{item.azureAdStatus}</div> 
)


const PersonnelColumns = ():  DataTableColumn<Personnel>[] => [
  {
    key: 'Name',
    accessor: 'name',
    label: 'Person',
    priority: 1,
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
    key: 'azureAdStatus',
    accessor: 'azureAdStatus',
    label: 'AD',
    priority: 10,
    component : AzureAdStatus,
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
    key: 'Workload',
    accessor:"hasCV",
    label: 'Workload',
    priority: 15,
    sortable: true,
  },
  {
    key: 'positions',
    accessor: 'hasCV',
    label: 'Positions',
    priority: 15,
    sortable: true,
  }      
]

export default PersonnelColumns
