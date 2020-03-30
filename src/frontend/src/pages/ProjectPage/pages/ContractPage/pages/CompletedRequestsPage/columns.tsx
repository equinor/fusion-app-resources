import { DataTableColumn, PersonCard } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import * as React from 'react';
import PositionColumn from '../../../../components/PositionColumn';
import { useHistory } from '@equinor/fusion';
import * as styles from './styles.less';
import RequestWorkflow from '../../components/RequestWorkflow';

type ColumnSideSheetLinkProps = {
    requestId: string;
};

const ColumnSideSheetLink: React.FC<ColumnSideSheetLinkProps> = ({ requestId, children }) => {
    const history = useHistory();

    const openSideSheet = React.useCallback(() => {
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
