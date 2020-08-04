import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../models/PersonDelegation';
import * as React from 'react';
import { formatDate } from '@equinor/fusion';

type AssignedPersonProps = {
    item: PersonDelegation;
};
const AssignedPersonComponent: React.FC<AssignedPersonProps> = ({ item }) => {
    return <PersonCard personId={item.person.azureUniquePersonId} photoSize="medium" inline />;
};
const columns: DataTableColumn<PersonDelegation>[] = [
    {
        key: 'role',
        label: 'Role',
        accessor: 'classification',
    },
    {
        key: 'delegated-to-person',
        label: 'Delegated to person',
        accessor: (d) => d.person.name,
        component: AssignedPersonComponent,
    },
    {
        key: 'valid-to',
        label: 'Valid to',
        accessor: (d) => formatDate(d.validTo),
    },
    {
        key: 're-certification-date',
        label: 'Re-certification date',
        accessor: (d) => (d.recertifiedDate ? formatDate(d.recertifiedDate) : '-'),
    },
    {
        key: 'Certified by',
        label: 'Certified by',
        accessor: (d) => d.createdBy.name,
        component: AssignedPersonComponent,
    },
];

export default columns;
