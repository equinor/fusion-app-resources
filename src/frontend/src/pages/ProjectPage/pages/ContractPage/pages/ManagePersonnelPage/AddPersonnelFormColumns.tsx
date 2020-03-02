import * as React from 'react';
import { DataTableColumn, TextInput } from '@equinor/fusion-components';
import Person from '../../../../../../models/Person';

export type DataItemProps = {
  item: Person;
  rowIndex: number;
};



const AddPersonnelFormColumns = ():  DataTableColumn<Person>[] => [
  {
    key: 'Name',
    accessor: 'name',
    label: 'Person',
    priority: 1,
  },
  {
    key: 'Mail',
    accessor: 'mail',
    label: 'E-Mail',
    priority: 5,
   
  },
  {
    key: 'Phone',
    accessor: 'phoneNumber',
    label: 'Phone Number',
    priority: 15,
  }
]

export default AddPersonnelFormColumns
