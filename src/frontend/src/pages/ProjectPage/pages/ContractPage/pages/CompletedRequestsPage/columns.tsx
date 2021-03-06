import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';

import PositionColumn from '../../../../components/PositionColumn';
import { useHistory, formatDateTime } from '@equinor/fusion';
import styles from './styles.less';
import RequestWorkflow from '../../components/RequestWorkflow';
import { FC, useCallback } from 'react';

type ColumnSideSheetLinkProps = {
    requestId: string;
};

const ColumnSideSheetLink: FC<ColumnSideSheetLinkProps> = ({ requestId, children }) => {
    const history = useHistory();

    const openSideSheet = useCallback(() => {
        const sideSheetSearchString = `requestId=${requestId}`;
        history.push({
            pathname: history.location.pathname,
            search: sideSheetSearchString,
        });
    }, [history.location.pathname, requestId]);

    return (
        <div onClick={openSideSheet} className={styles.columnLink}>
            {children}
        </div>
    );
};

const columns: DataTableColumn<PersonnelRequest>[] = [
    {
        accessor: request => request.position?.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>
                {item.position?.basePosition?.name || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: request => request.person?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: ({ item }) => (
            <PersonCard personId={item.person?.azureUniquePersonId} inline photoSize="medium" />
        ),
    },
    {
        accessor: request => request.state.toString(),
        key: 'status',
        label: 'Status',
        component: ({ item }) =>
            item.workflow && item.provisioningStatus ? (
                <RequestWorkflow
                    workflow={item.workflow}
                    inline
                    provisioningStatus={item.provisioningStatus}
                />
            ) : null,
        sortable: true,
    },
    {
        accessor: request => request.position?.name || 'TBN',
        key: 'position',
        label: 'Custom position title',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>
                {item.position?.name || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: request => (request.lastActivity ? formatDateTime(request.lastActivity) : 'N/A'),
        key: 'lastActivity',
        label: 'Last activity',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>
                {item.lastActivity ? formatDateTime(item.lastActivity) : 'N/A'}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: request => request.position?.basePosition?.discipline || 'TBN',
        key: 'discipline',
        label: 'Discipline',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>
                {item.position?.basePosition?.discipline || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: request => request.position?.taskOwner?.positionId || '',
        key: 'taskOwnerId',
        label: 'Task owner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId = item.position?.taskOwner?.positionId;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
];

export default columns;
