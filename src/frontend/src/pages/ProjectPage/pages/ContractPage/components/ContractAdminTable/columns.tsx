import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../models/PersonDelegation';

import { formatDate } from '@equinor/fusion';
import styles from './styles.less';
import { FC } from 'react';
import classNames from 'classnames';

type ColumnProps = {
    item: PersonDelegation;
};
const AssignedPersonComponent: FC<ColumnProps> = ({ item }) => {
    return <PersonCard personId={item.person.azureUniquePersonId} photoSize="medium" inline />;
};

const CertifiedByComponent: FC<ColumnProps> = ({ item }) => {
    const certifiedBy = item.recertifiedBy || item.createdBy;
    return <PersonCard personId={certifiedBy.azureUniquePersonId} photoSize="medium" inline />;
};

const ValidToComponent: FC<ColumnProps> = ({ item }) => {
    const oneDay = 24 * 60 * 60 * 1000;
    const today = new Date();
    const validTo = item.validTo;
    const diffDays = Math.round(Math.abs((today.getTime() - validTo.getTime()) / oneDay));
    const isOverDue = today.getTime() > validTo.getTime();

    const daysLeftClasses = classNames(styles.daysLeft, {
        [styles.overdue]: isOverDue,
    });
    
    if (diffDays > 30) {
        return <>{formatDate(validTo)}</>;
    }
    return (
        <div className={styles.validToContainer}>
            <span>{formatDate(validTo)}</span>
            <span className={daysLeftClasses}>
                {isOverDue ? 'Overdue' : `${diffDays} days left`}
            </span>
        </div>
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
        accessor: (d) => (d.recertifiedDate ? formatDate(d.recertifiedDate) : '-'),
    },
    {
        key: 'Certified by',
        label: 'Certified by',
        accessor: (d) => (d.recertifiedBy ? d.recertifiedBy.name : d.createdBy.name),
        component: CertifiedByComponent,
    },
];

export default columns;
