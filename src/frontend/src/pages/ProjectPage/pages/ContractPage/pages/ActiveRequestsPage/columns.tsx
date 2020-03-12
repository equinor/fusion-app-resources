import { DataTableColumn } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import RequestStateFlow from '../../components/RequestStateFlow';
import * as React from 'react';
import PositionColumn from '../../../../components/PositionColumn';
import { useHistory } from '@equinor/fusion';
import * as styles from './styles.less';

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
        accessor: request => request.person?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>{item.person?.name || ''}</ColumnSideSheetLink>
        ),
    },
    {
        accessor: request => request.state.toString(),
        key: 'status',
        label: 'Status',
        component: RequestStateFlow,
        sortable: true,
    },
    {
        accessor: request => request.position?.name || 'TBN',
        key: 'position',
        label: 'Position',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink requestId={item.id}>
                {item.position?.name || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },

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
        accessor: request =>
            request.position?.instances.find(i => i.parentPositionId)?.parentPositionId || '',
        key: 'taskOwnerId',
        label: 'Taskowner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId =
                item.position?.instances.find(i => i.parentPositionId)?.parentPositionId || null;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
];

export default columns;
