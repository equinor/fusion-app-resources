import { DataTableColumn } from '@equinor/fusion-components';
import Personnel from '../../../../../../models/Personnel';
import styles from './styles.less';
import PersonnelInfoSideSheet from './PersonnelInfoSideSheet';
import { FC, useState } from 'react';
import AzureAdStatusIndicator from '../../components/AzureAdStatusIndicator';
import HasEquinorMailCell from '../../components/HasEquinorMailCell';
import Tooltip from '@equinor/fusion-react-tooltip';

export type DataItemProps = {
    item: Personnel;
};

type ColumnPersonnelInfoSideSheetLinkProps = {
    person: Personnel;
    maxWidth?: string;
    tooltip?: string | null;
};

const ColumnPersonnelInfoSideSheetLink: FC<ColumnPersonnelInfoSideSheetLinkProps> = ({
    person,
    children,
    maxWidth,
    tooltip,
}) => {
    const [isOpen, setIsOpen] = useState<boolean>(false);

    return (
        <Tooltip content={tooltip} disabled={!tooltip}>
            <div
                onClick={() => setIsOpen(!isOpen)}
                className={styles.columnLink}
                style={{ maxWidth }}
            >
                {children}
                <PersonnelInfoSideSheet person={person} isOpen={isOpen} setIsOpen={setIsOpen} />
            </div>
        </Tooltip>
    );
};

const PersonnelColumns = (contractId?: string | null): DataTableColumn<Personnel>[] => [
    {
        key: 'azureAdStatus',
        accessor: 'azureAdStatus',
        label: 'AD',
        priority: 2,
        component: ({ item }) => (
            <AzureAdStatusIndicator status={item?.azureAdStatus || 'NoAccount'} />
        ),
        sortable: true,
        width: '20px',
    },
    {
        key: 'upn',
        accessor: 'upn',
        label: 'UPN',
        component: ({ item }) => (
            <ColumnPersonnelInfoSideSheetLink person={item} maxWidth="15rem" tooltip={item.upn}>
                {item.upn}
            </ColumnPersonnelInfoSideSheetLink>
        ),
        priority: 1,
        sortable: true,
    },
    {
        key: 'FirstName',
        accessor: (p) => p.firstName || '',
        label: 'First Name',
        component: ({ item }) => (
            <ColumnPersonnelInfoSideSheetLink person={item}>
                {item.firstName || ''}
            </ColumnPersonnelInfoSideSheetLink>
        ),
        priority: 3,
        sortable: true,
    },
    {
        key: 'LastName',
        accessor: (p) => p.lastName || '',
        label: 'Last Name',
        component: ({ item }) => (
            <ColumnPersonnelInfoSideSheetLink person={item}>
                {item.lastName || ''}
            </ColumnPersonnelInfoSideSheetLink>
        ),
        priority: 4,
        sortable: true,
    },
    {
        key: 'Disciplines',
        accessor: (p) => p.disciplines.map((d) => d.name)?.join('/') || '',
        label: 'Discipline',
        priority: 5,
        sortable: true,
    },
    {
        key: 'Phone',
        accessor: 'phoneNumber',
        label: 'Phone Number',
        priority: 7,
        sortable: true,
    },
    {
        key: 'Workload',
        accessor: (r) => '-',
        label: 'Workload',
        priority: 8,
        component: (p) => (
            <span>{`${(
                p.item.positions?.reduce((val, pos) => {
                    return (val += pos.workload);
                }, 0) || 0
            ).toString()}%`}</span>
        ),
        sortable: true,
        width: '20px',
    },
    {
        key: 'positions',
        accessor: 'personnelId',
        label: 'Nr Positions',
        priority: 9,
        component: (p) => <span>{(p.item.positions?.length || 0).toString()}</span>,
        sortable: true,
        width: '20px',
    },
    {
        key: 'equinorMail',
        accessor: 'mail',
        label: 'Equinor mail',
        priority: 10,
        component: HasEquinorMailCell,
        sortable: true,
        width: '20px',
    },
    {
        key: 'Mail',
        accessor: 'mail',
        label: 'E-Mail',
        component: ({ item }) => (
            <ColumnPersonnelInfoSideSheetLink person={item}>
                {item.mail}
            </ColumnPersonnelInfoSideSheetLink>
        ),
        priority: 1,
        sortable: true,
    },
];

export default PersonnelColumns;
