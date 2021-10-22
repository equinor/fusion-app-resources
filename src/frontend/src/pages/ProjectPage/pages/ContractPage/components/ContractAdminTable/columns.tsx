import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../models/PersonDelegation';

import { formatDate } from '@equinor/fusion';
import styles from './styles.less';
import { FC } from 'react';

type ColumnProps = {
    item: PersonDelegation;
};
const AssignedPersonComponent: FC<ColumnProps> = ({ item }) => {
    return (
        <div data-cy="assigned-person">
            <PersonCard personId={item.person.azureUniquePersonId} photoSize="medium" inline />;
        </div>
    );    
};

const CertifiedByComponent: FC<ColumnProps> = ({ item }) => {
    const certifiedBy = item.recertifiedBy || item.createdBy
    return (
        <div data-cy="certified-by-person">
            <PersonCard personId={certifiedBy.azureUniquePersonId} photoSize="medium" inline />;
        </div>
    ); 
};

const ValidToComponent: FC<ColumnProps> = ({ item }) => {
    const oneDay = 24 * 60 * 60 * 1000;
    const today = new Date();
    const validTo = item.validTo;
    const diffDays = Math.round(Math.abs((today.getTime() - validTo.getTime()) / oneDay));

    if (diffDays > 30) {
        return <div data-cy="valid-to-date">{formatDate(validTo)}</div>;
    }
    return (
        <div data-cy="valid-to-date" className={styles.validToContainer}>
            <span>{formatDate(validTo)}</span>
            <span className={styles.daysLeft}>{`${diffDays} days left`}</span>
        </div>
    );
};

const RecertifyToComponent: FC<ColumnProps> = ({ item }) => {
    if (item.recertifiedDate) {
        return <div data-cy="recertification-date">{formatDate(item.recertifiedDate)}</div>;
    }
    return (
        <div data-cy="recertification-date">-</div>
    );
};

const columns: DataTableColumn<PersonDelegation>[] = [
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
        accessor: (d) => (d.recertifiedDate ? formatDate(d.recertifiedDate) : ''),
        component: RecertifyToComponent,
    },
    {
        key: 'Certified by',
        label: 'Certified by',
        accessor: (d) => d.recertifiedBy ? d.recertifiedBy.name : d.createdBy.name,
        component: CertifiedByComponent,
    },
];

export default columns;
