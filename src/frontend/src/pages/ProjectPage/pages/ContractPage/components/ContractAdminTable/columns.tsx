import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../models/PersonDelegation';
import * as React from 'react';
import { formatDate } from '@equinor/fusion';
import * as styles from './styles.less';

type ColumnProps = {
    item: PersonDelegation;
};
const AssignedPersonComponent: React.FC<ColumnProps> = ({ item }) => {
    return <PersonCard personId={item.person.azureUniquePersonId} photoSize="medium" inline />;
};

const ValidToComponent: React.FC<ColumnProps> = ({ item }) => {
    const oneDay = 24 * 60 * 60 * 1000;
    const today = new Date();
    const validTo = item.validTo;
    const diffDays = Math.round(Math.abs((today.getTime() - validTo.getTime()) / oneDay));

    if (diffDays > 30) {
        return <>{formatDate(validTo)}</>;
    }
    return (
        <div className={styles.validToContainer}>
            <span>{formatDate(validTo)}</span>
            <span className={styles.daysLeft}>{`${diffDays} days left`}</span>
        </div>
    );
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
        component: ValidToComponent,
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
